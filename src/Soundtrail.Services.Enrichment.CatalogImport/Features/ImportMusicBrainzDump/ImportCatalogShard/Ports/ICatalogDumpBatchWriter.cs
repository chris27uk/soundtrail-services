using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

public interface ICatalogDumpBatchWriter
{
    Task FlushAsync(
        IReadOnlyList<CatalogDumpBatchItem> items,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default);
}

public abstract record CatalogDumpBatchItem;

public sealed record ArtistDumpBatchItem(Artist Artist) : CatalogDumpBatchItem;

public sealed record AlbumDumpBatchItem(Album Album) : CatalogDumpBatchItem;

public sealed record TrackDumpBatchItem(Track Track) : CatalogDumpBatchItem;
