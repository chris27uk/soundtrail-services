using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Operations;

namespace Soundtrail.Services.Internal.Projector.Features.OnLookupRecorded;

public sealed class DiscoveryOutcomeProjectorHandler(
    ICommandBus commandBus,
    IEventStreamRepository<ArtistId> repository)
{
    public async Task Handle(ArtistDiscovered @event, CancellationToken cancellationToken = default)
    {
        var (stream, catalog) = await ArtistCatalog.LoadAsync(repository, @event.Artist.Id, cancellationToken);
        catalog.CatalogItemDiscovered(new Domain.Catalog.CatalogItem.MusicArtist(@event.Artist));
        await catalog.SaveAsync(repository, stream, CommandId.For($"ArtistDiscovered:{@event.Artist.Id.Value}:{@event.ObservedAt:O}"), cancellationToken);
    }

    public async Task Handle(AlbumDiscovered @event, CancellationToken cancellationToken = default)
    {
        var artistId = ArtistId.From(@event.Album.AlbumId.ArtistId);
        var (stream, catalog) = await ArtistCatalog.LoadAsync(repository, artistId, cancellationToken);
        catalog.CatalogItemDiscovered(new Domain.Catalog.CatalogItem.MusicAlbum(@event.Album));
        await catalog.SaveAsync(repository, stream, CommandId.For($"AlbumDiscovered:{@event.Album.AlbumId.StableValue}:{@event.ObservedAt:O}"), cancellationToken);
    }

    public async Task Handle(TrackDiscovered @event, CancellationToken cancellationToken = default)
    {
        var artistId = @event.Hierarchy.ArtistId
            ?? (@event.Hierarchy.AlbumId is { } albumId
                ? ArtistId.From(albumId.ArtistId)
                : throw new InvalidOperationException("TrackDiscovered must include artist ownership hierarchy."));

        var (stream, catalog) = await ArtistCatalog.LoadAsync(repository, artistId, cancellationToken);
        catalog.CatalogItemDiscovered(new Domain.Catalog.CatalogItem.MusicTrack(@event.Track));
        await catalog.SaveAsync(repository, stream, CommandId.For($"TrackDiscovered:{@event.Track.TrackId.Value}:{@event.ObservedAt:O}"), cancellationToken);
    }

    public async Task Handle(PlaylistTracksDiscovered @event, CancellationToken cancellationToken = default)
    {
        await commandBus.SendAsync(
            new PlaylistUpdated(@event.PlaylistId.Value, @event.Tracks)
            {
                CommandId = CommandId.For($"PlaylistUpdated:{@event.PlaylistId.Value}:{@event.ObservedAt:O}"),
                CorrelationId = CorrelationId.From($"playlist-tracks-discovered:{@event.PlaylistId.Value}:{@event.ObservedAt:O}"),
                CreatedAt = @event.ObservedAt
            },
            cancellationToken);
    }
}
