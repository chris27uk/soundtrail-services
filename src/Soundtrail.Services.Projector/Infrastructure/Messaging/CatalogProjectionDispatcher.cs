using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogItemChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal sealed class CatalogProjectionDispatcher(
    StoredEventDomainEventResolver resolver,
    ArtistCatalogChangedProjectorHandler artistCatalogChangedProjectorHandler,
    CatalogItemChangedProjectorHandler catalogItemChangedProjectorHandler,
    CatalogSearchCandidateChangedProjectorHandler catalogSearchCandidateChangedProjectorHandler,
    CatalogTrackChangedProjectorHandler catalogTrackChangedProjectorHandler,
    PlaylistTracksDiscoveredProjectorHandler playlistTracksDiscoveredProjectorHandler)
{
    private readonly EventHandlers catalogStreamHandlers = BuildCatalogStreamHandlers(
        artistCatalogChangedProjectorHandler,
        catalogItemChangedProjectorHandler,
        catalogSearchCandidateChangedProjectorHandler,
        catalogTrackChangedProjectorHandler,
        playlistTracksDiscoveredProjectorHandler);

    public async Task DispatchAsync(RavenStoredEventRecord storedEvent, CancellationToken cancellationToken)
    {
        var @event = resolver.Resolve(storedEvent);

        switch (storedEvent.AggregateType)
        {
            case "catalog-stream":
                await catalogStreamHandlers.HandleAsync(@event, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unsupported catalog aggregate type '{storedEvent.AggregateType}'.");
        }
    }

    private static EventHandlers BuildCatalogStreamHandlers(
        ArtistCatalogChangedProjectorHandler artistCatalogChangedProjectorHandler,
        CatalogItemChangedProjectorHandler catalogItemChangedProjectorHandler,
        CatalogSearchCandidateChangedProjectorHandler catalogSearchCandidateChangedProjectorHandler,
        CatalogTrackChangedProjectorHandler catalogTrackChangedProjectorHandler,
        PlaylistTracksDiscoveredProjectorHandler playlistTracksDiscoveredProjectorHandler)
    {
        var handlers = new EventHandlers();

        handlers.RegisterAsync<ArtistDiscovered>(catalogItemChangedProjectorHandler.Handle);
        handlers.RegisterAsync<ArtistDiscovered>((@event, cancellationToken) =>
            artistCatalogChangedProjectorHandler.Handle(@event.Artist.Id, cancellationToken));
        handlers.RegisterAsync<ArtistDiscovered>(catalogSearchCandidateChangedProjectorHandler.Handle);

        handlers.RegisterAsync<AlbumDiscovered>(catalogItemChangedProjectorHandler.Handle);
        handlers.RegisterAsync<AlbumDiscovered>((@event, cancellationToken) =>
            artistCatalogChangedProjectorHandler.Handle(Soundtrail.Domain.Catalog.Artists.ArtistId.From(@event.Album.AlbumId.ArtistId), cancellationToken));
        handlers.RegisterAsync<AlbumDiscovered>(catalogSearchCandidateChangedProjectorHandler.Handle);

        handlers.RegisterAsync<TrackDiscovered>(catalogItemChangedProjectorHandler.Handle);
        handlers.RegisterAsync<TrackDiscovered>((@event, cancellationToken) =>
            artistCatalogChangedProjectorHandler.Handle(
                @event.Hierarchy.ArtistId
                ?? (@event.Hierarchy.AlbumId is { } albumId
                    ? Soundtrail.Domain.Catalog.Artists.ArtistId.From(albumId.ArtistId)
                    : throw new InvalidOperationException("TrackDiscovered must include artist ownership hierarchy.")),
                cancellationToken));
        handlers.RegisterAsync<TrackDiscovered>((@event, cancellationToken) =>
            catalogTrackChangedProjectorHandler.Handle(@event.Track.TrackId, cancellationToken));
        handlers.RegisterAsync<TrackDiscovered>(catalogSearchCandidateChangedProjectorHandler.Handle);

        handlers.RegisterAsync<StreamingLocationDiscovered>(catalogItemChangedProjectorHandler.Handle);
        handlers.RegisterAsync<StreamingLocationDiscovered>((@event, cancellationToken) =>
            artistCatalogChangedProjectorHandler.Handle(
                @event.Hierarchy.ArtistId ?? throw new InvalidOperationException("StreamingLocationDiscovered must include artist ownership hierarchy."),
                cancellationToken));

        handlers.RegisterAsync<PlaylistTracksDiscovered>(playlistTracksDiscoveredProjectorHandler.Handle);

        return handlers;
    }
}
