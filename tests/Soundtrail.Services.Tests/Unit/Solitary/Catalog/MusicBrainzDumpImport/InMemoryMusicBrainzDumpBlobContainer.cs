using System.Runtime.CompilerServices;
using System.Text;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

/// <summary>
/// In-memory blob container for solitary dump storage tests.
/// </summary>
internal sealed class InMemoryMusicBrainzDumpBlobContainer : IMusicBrainzDumpBlobContainer
{
    private readonly Dictionary<string, byte[]> blobs = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> BlobNames => blobs.Keys;

    public Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(blobs.ContainsKey(blobName));
    }

    public async Task UploadFromFileAsync(
        string blobName,
        string localFilePath,
        CancellationToken cancellationToken = default)
    {
        var bytes = await File.ReadAllBytesAsync(localFilePath, cancellationToken);
        blobs[blobName] = bytes;
    }

    public async Task DownloadToFileAsync(
        string blobName,
        string localFilePath,
        CancellationToken cancellationToken = default)
    {
        if (!blobs.TryGetValue(blobName, out var bytes))
        {
            throw new FileNotFoundException($"Blob '{blobName}' was not found.", blobName);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(localFilePath))!);
        await File.WriteAllBytesAsync(localFilePath, bytes, cancellationToken);
    }

    public Task UploadLinesAsync(
        string blobName,
        IReadOnlyList<string> lines,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var content = string.Join('\n', lines);
        if (lines.Count > 0)
        {
            content += '\n';
        }

        blobs[blobName] = Encoding.UTF8.GetBytes(content);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> ReadLinesAsync(
        string blobName,
        long skipLines,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!blobs.TryGetValue(blobName, out var bytes))
        {
            yield break;
        }

        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8);
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
}
