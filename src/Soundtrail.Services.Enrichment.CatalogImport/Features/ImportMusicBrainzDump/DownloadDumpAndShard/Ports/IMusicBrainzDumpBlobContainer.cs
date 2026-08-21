namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

/// <summary>
/// Thin blob container surface for dump archives and shards (Azurite or Azure).
/// </summary>
public interface IMusicBrainzDumpBlobContainer
{
    Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default);

    Task UploadFromFileAsync(
        string blobName,
        string localFilePath,
        CancellationToken cancellationToken = default);

    Task DownloadToFileAsync(
        string blobName,
        string localFilePath,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ReadLinesAsync(
        string blobName,
        long skipLines,
        CancellationToken cancellationToken = default);
}
