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

    /// <summary>
    /// Streams denormalized track JSONL lines without writing a full <c>release.jsonl</c> or
    /// <c>track.jsonl</c> when those caches are absent. Prefers a cached track file; otherwise
    /// streams the official release archive.
    /// </summary>
    IAsyncEnumerable<string> ReadDenormalizedTrackLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams official artist JSONL without requiring a materialized <c>artist.jsonl</c> extract.
    /// </summary>
    IAsyncEnumerable<string> ReadArtistLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams official release-group JSONL without requiring a materialized extract.
    /// </summary>
    IAsyncEnumerable<string> ReadReleaseGroupLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams official release JSONL without requiring a materialized <c>release.jsonl</c> extract.
    /// Used by the Recordings producer so join/skip can happen on release lines.
    /// </summary>
    IAsyncEnumerable<string> ReadReleaseLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when denormalized track JSONL is available without joining the release graph
    /// (cached track file or track archive).
    /// </summary>
    bool HasCachedDenormalizedTrackSource(string dumpVersion);
}
