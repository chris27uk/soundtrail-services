using System.Net.Http;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class HttpMusicBrainzDumpDownloader(HttpClient httpClient) : Ports.IMusicBrainzDumpDownloader
{
    public async Task DownloadAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (File.Exists(destinationPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);

        var tempPath = destinationPath + ".partial";
        try
        {
            await using var responseStream = await httpClient.GetStreamAsync(url, cancellationToken);
            await using var fileStream = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81_920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await responseStream.CopyToAsync(fileStream, cancellationToken);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }

        File.Move(tempPath, destinationPath, overwrite: true);
    }
}
