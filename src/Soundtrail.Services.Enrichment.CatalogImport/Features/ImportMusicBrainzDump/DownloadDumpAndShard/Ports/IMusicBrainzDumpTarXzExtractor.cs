namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

public interface IMusicBrainzDumpTarXzExtractor
{
    /// <summary>
    /// Extracts the JSONL member for <paramref name="entityName"/> from <paramref name="archivePath"/>
    /// into <paramref name="outputJsonlPath"/>. No-op when the output file already exists.
    /// </summary>
    void EnsureExtracted(string archivePath, string entityName, string outputJsonlPath);
}
