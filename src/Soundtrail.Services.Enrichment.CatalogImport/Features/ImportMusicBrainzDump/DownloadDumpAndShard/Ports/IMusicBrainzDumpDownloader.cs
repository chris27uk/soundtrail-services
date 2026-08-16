namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

public interface IMusicBrainzDumpDownloader
{
    /// <summary>
    /// Downloads <paramref name="url"/> to <paramref name="destinationPath"/>.
    /// Skips when the destination already exists. Resumes from a sibling <c>.partial</c> file via HTTP Range when present.
    /// </summary>
    Task DownloadAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken = default);
}
