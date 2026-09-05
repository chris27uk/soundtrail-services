namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

public interface IMusicBrainzDumpTarXzExtractor
{
    /// <summary>
    /// Extracts the JSONL member for <paramref name="entityName"/> from <paramref name="archivePath"/>
    /// into <paramref name="outputJsonlPath"/>. No-op when the output file already exists.
    /// </summary>
    void EnsureExtracted(string archivePath, string entityName, string outputJsonlPath);

    /// <summary>
    /// Streams JSONL lines for <paramref name="entityName"/> from the archive without writing an extracted file.
    /// </summary>
    IAsyncEnumerable<string> ReadJsonlLinesAsync(
        string archivePath,
        string entityName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams JSONL lines from an already-open archive stream (file, blob, or HTTP).
    /// Does not dispose <paramref name="archiveStream"/>.
    /// </summary>
    IAsyncEnumerable<string> ReadJsonlLinesAsync(
        Stream archiveStream,
        string entityName,
        string sourceName,
        CancellationToken cancellationToken = default);
}
