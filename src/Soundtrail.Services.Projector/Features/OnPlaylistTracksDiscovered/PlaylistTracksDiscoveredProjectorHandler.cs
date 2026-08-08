using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered;

public sealed class PlaylistTracksDiscoveredProjectorHandler(
    IStorePlaylistTracksReadModelPort storePlaylistTracksReadModelPort)
{
    public async Task Handle(PlaylistTracksDiscovered @event, CancellationToken cancellationToken = default)
    {
        await storePlaylistTracksReadModelPort.StoreAsync(@event, cancellationToken);
    }
}
