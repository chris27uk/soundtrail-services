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

    /// <summary>
    /// Opens <paramref name="url"/> as a readable stream without buffering the whole payload on disk.
    /// </summary>
    Task<Stream> OpenReadAsync(
        string url,
        CancellationToken cancellationToken = default);
}
