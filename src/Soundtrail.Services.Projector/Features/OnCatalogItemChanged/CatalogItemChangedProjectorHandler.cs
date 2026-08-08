using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogItemChanged;

public sealed class CatalogItemChangedProjectorHandler(IEventStreamRepository<ArtistId> repository) :
    IProjectionEventHandler<ArtistDiscovered>,
    IProjectionEventHandler<AlbumDiscovered>,
    IProjectionEventHandler<TrackDiscovered>,
    IProjectionEventHandler<StreamingLocationDiscovered>
{
    Task IProjectionEventHandler<ArtistDiscovered>.HandleAsync(ArtistDiscovered @event, CancellationToken cancellationToken) =>
        Handle(@event, cancellationToken);

    Task IProjectionEventHandler<AlbumDiscovered>.HandleAsync(AlbumDiscovered @event, CancellationToken cancellationToken) =>
        Handle(@event, cancellationToken);

    Task IProjectionEventHandler<TrackDiscovered>.HandleAsync(TrackDiscovered @event, CancellationToken cancellationToken) =>
        Handle(@event, cancellationToken);

    Task IProjectionEventHandler<StreamingLocationDiscovered>.HandleAsync(
        StreamingLocationDiscovered @event,
        CancellationToken cancellationToken) =>
        Handle(@event, cancellationToken);

    public async Task Handle(ArtistDiscovered @event, CancellationToken cancellationToken = default)
    {
        var (stream, catalog) = await ArtistCatalog.LoadAsync(repository, @event.Artist.Id, cancellationToken);
        catalog.CatalogItemDiscovered(new Domain.Catalog.CatalogItem.MusicArtist(@event.Artist));
        await catalog.SaveAsync(repository, stream, MessageId.For($"ArtistDiscovered:{@event.Artist.Id.Value}:{@event.ObservedAt:O}"), cancellationToken);
    }

    public async Task Handle(AlbumDiscovered @event, CancellationToken cancellationToken = default)
    {
        var artistId = ArtistId.From(@event.Album.AlbumId.ArtistId);
        var (stream, catalog) = await ArtistCatalog.LoadAsync(repository, artistId, cancellationToken);
        catalog.CatalogItemDiscovered(new Domain.Catalog.CatalogItem.MusicAlbum(@event.Album));
        await catalog.SaveAsync(repository, stream, MessageId.For($"AlbumDiscovered:{@event.Album.AlbumId.StableValue}:{@event.ObservedAt:O}"), cancellationToken);
    }

    public async Task Handle(TrackDiscovered @event, CancellationToken cancellationToken = default)
    {
        var artistId = @event.Hierarchy.ArtistId
            ?? (@event.Hierarchy.AlbumId is { } albumId
                ? ArtistId.From(albumId.ArtistId)
                : throw new InvalidOperationException("TrackDiscovered must include artist ownership hierarchy."));

        var (stream, catalog) = await ArtistCatalog.LoadAsync(repository, artistId, cancellationToken);
        catalog.CatalogItemDiscovered(new Domain.Catalog.CatalogItem.MusicTrack(@event.Track));
        await catalog.SaveAsync(repository, stream, MessageId.For($"TrackDiscovered:{@event.Track.TrackId.Value}:{@event.ObservedAt:O}"), cancellationToken);
    }

    public async Task Handle(StreamingLocationDiscovered @event, CancellationToken cancellationToken = default)
    {
        var artistId = @event.Hierarchy.ArtistId ?? throw new InvalidOperationException("StreamingLocationDiscovered must include artist ownership hierarchy.");
        var trackId = @event.MusicCatalogId.AsTrack();
        var (stream, catalog) = await ArtistCatalog.LoadAsync(repository, artistId, cancellationToken);
        
        catalog.StreamingLocationDiscovered(
            trackId,
            new Domain.Catalog.StreamingLocation(
                @event.Provider,
                @event.ExternalId,
                @event.Url,
                @event.SourceProvider,
                @event.ObservedAt));
        
        await catalog.SaveAsync(
            repository,
            stream,
            MessageId.For($"StreamingLocationDiscovered:{artistId.Value}:{trackId.Value}:{@event.ObservedAt:O}"),
            cancellationToken);
    }
}
