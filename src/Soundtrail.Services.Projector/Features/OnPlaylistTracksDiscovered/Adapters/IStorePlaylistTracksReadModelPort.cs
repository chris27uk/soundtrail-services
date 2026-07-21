using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

public interface IStorePlaylistTracksReadModelPort
{
    Task StoreAsync(PlaylistTracksDiscovered @event, CancellationToken cancellationToken);

    Task RepairTrackAsync(TrackId trackId, CancellationToken cancellationToken);
}
