using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogAlbumImportWriter(
    IEventStreamRepository<ArtistId> artistRepository,
    IDocumentStore documentStore) : ICatalogAlbumImportWriter
{
    public async Task WriteAsync(
        Album album,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(album);

        var artistId = ArtistId.From(album.AlbumId.ArtistId);
        var (stream, catalog) = await ArtistCatalog.LoadAsync(artistRepository, artistId, cancellationToken);
        var existing = stream.Events
            .OfType<AlbumDiscovered>()
            .LastOrDefault(@event => @event.Album.AlbumId.StableValue == album.AlbumId.StableValue);
        if (existing is not null && existing.ObservedAt >= dumpObservedAt)
        {
            return;
        }

        var albumToWrite = new Album(
            album.AlbumId,
            album.AlbumTitle,
            album.SourceSystemIds,
            album.ReleaseDate,
            album.ArtworkUrl,
            dumpObservedAt);

        catalog.CatalogItemDiscovered(new CatalogItem.MusicAlbum(albumToWrite));
        await catalog.SaveAsync(
            artistRepository,
            stream,
            MessageId.For($"bulk-import:AlbumDiscovered:{album.AlbumId.StableValue}:{dumpObservedAt:O}"),
            cancellationToken,
            ProjectionHint.BulkImport);

        await StoreAlbumReadModelAsync(artistId, albumToWrite, dumpObservedAt, cancellationToken);
    }

    private async Task StoreAlbumReadModelAsync(
        ArtistId artistId,
        Album album,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var documentId = CatalogArtistAlbumsRecordDto.GetDocumentId(artistId.Value);
        var existing = await session.LoadAsync<CatalogArtistAlbumsRecordDto>(documentId, cancellationToken)
            ?? new CatalogArtistAlbumsRecordDto
            {
                Id = documentId,
                ArtistId = artistId.Value,
                ArtistName = string.Empty,
                Albums = []
            };

        var albums = existing.Albums
            .Where(item => !string.Equals(item.AlbumId, album.AlbumId.StableValue, StringComparison.Ordinal))
            .Append(
                new CatalogArtistAlbumRecordDto
                {
                    AlbumId = album.AlbumId.StableValue,
                    MusicCatalogId = album.AlbumId.StableValue,
                    AlbumTitle = album.AlbumTitle ?? string.Empty,
                    ReleaseDate = album.ReleaseDate,
                    ArtworkUrl = album.ArtworkUrl
                })
            .OrderBy(static item => item.ReleaseDate)
            .ThenBy(static item => item.AlbumTitle, StringComparer.Ordinal)
            .ToArray();

        existing.Albums = albums;
        existing.UpdatedAt = updatedAt;
        await session.StoreAsync(existing, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }
}
