using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

public interface IMusicBrainzDumpArchiveStore
{
    /// <summary>
    /// Ensures the artists JSONL source for the dump exists (configured path, cache, or HTTP download).
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

    /// <summary>
    /// Ensures the official release JSONL source exists. Returns the path/key to the file.
    /// </summary>
    Task<string> EnsureReleasesJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the denormalized track-graph JSONL source exists (cached track archive,
    /// or materialized from the official release graph). Returns the path/key to the file.
    /// </summary>
    Task<string> EnsureTracksJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default);
}
