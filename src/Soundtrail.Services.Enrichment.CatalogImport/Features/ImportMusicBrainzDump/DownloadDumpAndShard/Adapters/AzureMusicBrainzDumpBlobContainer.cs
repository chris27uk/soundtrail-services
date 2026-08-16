using System.Runtime.CompilerServices;
using System.Text;
using Azure.Storage.Blobs;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class AzureMusicBrainzDumpBlobContainer(BlobContainerClient containerClient)
    : IMusicBrainzDumpBlobContainer
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool containerReady;

    public async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);
        var response = await containerClient.GetBlobClient(blobName).ExistsAsync(cancellationToken);
        return response.Value;
    }

    public async Task UploadFromFileAsync(
        string blobName,
        string localFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);
        await EnsureContainerAsync(cancellationToken);
        await containerClient.GetBlobClient(blobName).UploadAsync(
            localFilePath,
            overwrite: true,
            cancellationToken);
    }

    public async Task DownloadToFileAsync(
        string blobName,
        string localFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);
        await EnsureContainerAsync(cancellationToken);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(localFilePath))!);
        await containerClient.GetBlobClient(blobName).DownloadToAsync(localFilePath, cancellationToken);
    }

    public async Task UploadLinesAsync(
        string blobName,
        IReadOnlyList<string> lines,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lines);
        await EnsureContainerAsync(cancellationToken);

        await using var stream = new MemoryStream();
        await using (var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            foreach (var line in lines)
            {
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
            }

            await writer.FlushAsync(cancellationToken);
        }

        stream.Position = 0;
        await containerClient.GetBlobClient(blobName).UploadAsync(stream, overwrite: true, cancellationToken);
    }

    public async IAsyncEnumerable<string> ReadLinesAsync(
        string blobName,
        long skipLines,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);
        var blob = containerClient.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(cancellationToken))
        {
            yield break;
        }

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        await using var content = download.Value.Content;
        using var reader = new StreamReader(content, Encoding.UTF8);

        long lineNumber = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            if (lineNumber++ < skipLines)
            {
                continue;
            }

            yield return line;
        }
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (containerReady)
        {
            return;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (containerReady)
            {
                return;
            }

            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            containerReady = true;
        }
        finally
        {
            gate.Release();
        }
    }
}
