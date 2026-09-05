using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

public interface IMusicBrainzDumpShardStore
{
    IMusicBrainzDumpShardWriter OpenWriter(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardCount,
        bool append = false);

    IAsyncEnumerable<MusicBrainzDumpShardLine> ReadShardLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long skipLines,
        long skipBytes = 0,
        CancellationToken cancellationToken = default);

    IMusicBrainzDumpArtistIdSidecarWriter OpenArtistIdSidecarWriter(
        MusicBrainzDumpImportJobId jobId,
        int shardCount,
        bool append = false);

    /// <summary>
    /// Opens (or replaces) the compact id sidecar for a single Artists shard.
    /// </summary>
    IMusicBrainzDumpArtistIdSidecarWriter OpenSingleArtistIdSidecarWriter(
        MusicBrainzDumpImportJobId jobId,
        int shardId);

    /// <summary>
    /// Reads artist ids for a shard from the <c>.ids</c> sidecar when present; otherwise empty.
    /// </summary>
    IAsyncEnumerable<string> ReadArtistIdSidecarAsync(
        MusicBrainzDumpImportJobId jobId,
        int shardId,
        CancellationToken cancellationToken = default);

    bool ArtistIdSidecarExists(MusicBrainzDumpImportJobId jobId, int shardId);
}

public interface IMusicBrainzDumpArtistIdSidecarWriter : IAsyncDisposable
{
    Task AppendAsync(int shardId, string artistId, CancellationToken cancellationToken = default);
}

public interface IMusicBrainzDumpShardWriter : IAsyncDisposable
{
    Task AppendAsync(int shardId, string line, CancellationToken cancellationToken = default);

    Task CompleteAsync(CancellationToken cancellationToken = default);
}
