using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

public interface ICatalogDumpBatchWriter
{
    Task<IReadOnlySet<ArtistId>> AppendEventsAsync(
        IReadOnlyList<CatalogDumpBatchItem> items,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default);

    Task ProjectArtistsAsync(
        IReadOnlySet<ArtistId> artistIds,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams artist ids in <paramref name="shardId"/> whose catalog stream is ahead of
    /// the last projected version (or never projected).
    /// </summary>
    IAsyncEnumerable<ArtistId> EnumerateArtistsNeedingProjectionForShardAsync(
        int shardId,
        int shardCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears <c>ProjectedStreamVersion</c> and projection snapshots so artists are
    /// re-materialized after a truncated or stale projection (for example Raven event page limits).
    /// </summary>
    Task<int> ClearProjectedStreamVersionsAsync(CancellationToken cancellationToken = default);
}

public abstract record CatalogDumpBatchItem;

public sealed record ArtistDumpBatchItem(Artist Artist) : CatalogDumpBatchItem;

public sealed record AlbumDumpBatchItem(Album Album) : CatalogDumpBatchItem;

public sealed record TrackDumpBatchItem(Track Track) : CatalogDumpBatchItem;
