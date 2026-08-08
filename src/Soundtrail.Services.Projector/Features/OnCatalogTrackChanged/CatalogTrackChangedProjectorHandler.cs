using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged;

public sealed class CatalogTrackChangedProjectorHandler(
    IStorePlaylistTracksReadModelPort storePlaylistTracksReadModelPort) :
    IProjectionEventHandler<TrackDiscovered>,
    IProjectionEventHandler<StreamingLocationDiscovered>
{
    Task IProjectionEventHandler<TrackDiscovered>.HandleAsync(
        TrackDiscovered @event,
        CancellationToken cancellationToken) =>
        Handle(@event.Track.TrackId, cancellationToken);

    Task IProjectionEventHandler<StreamingLocationDiscovered>.HandleAsync(
        StreamingLocationDiscovered @event,
        CancellationToken cancellationToken) =>
        Handle(@event.MusicCatalogId.AsTrack(), cancellationToken);

    public Task Handle(TrackId trackId, CancellationToken cancellationToken = default) =>
        storePlaylistTracksReadModelPort.RepairTrackAsync(trackId, cancellationToken);
}
