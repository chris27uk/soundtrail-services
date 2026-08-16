namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

public interface IMusicBrainzDumpDownloader
{
    /// <summary>
    /// Downloads <paramref name="url"/> to <paramref name="destinationPath"/>.
    /// Skips the download when the destination file already exists.
    /// </summary>
    Task DownloadAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken = default);
}
