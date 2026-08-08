using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered;

public sealed class PlaylistTracksDiscoveredProjectorHandler(
    IStorePlaylistTracksReadModelPort storePlaylistTracksReadModelPort) :
    IProjectionEventHandler<PlaylistTracksDiscovered>
{
    Task IProjectionEventHandler<PlaylistTracksDiscovered>.HandleAsync(
        PlaylistTracksDiscovered @event,
        CancellationToken cancellationToken) =>
        Handle(@event, cancellationToken);

    public async Task Handle(PlaylistTracksDiscovered @event, CancellationToken cancellationToken = default)
    {
        await storePlaylistTracksReadModelPort.StoreAsync(@event, cancellationToken);
    }
}
