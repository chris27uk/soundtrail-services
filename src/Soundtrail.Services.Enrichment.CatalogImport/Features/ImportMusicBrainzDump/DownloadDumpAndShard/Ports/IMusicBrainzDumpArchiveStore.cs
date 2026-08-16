using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

public interface IMusicBrainzDumpArchiveStore
{
    /// <summary>
    /// Ensures the artists JSONL source for the dump exists (local fixture or previously downloaded).
    /// Returns the path/key to the artists JSONL file.
    /// </summary>
    Task<string> EnsureArtistsJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the release-group JSONL source exists. Returns the path/key to the file.
    /// </summary>
    Task<string> EnsureReleaseGroupsJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default);
}
