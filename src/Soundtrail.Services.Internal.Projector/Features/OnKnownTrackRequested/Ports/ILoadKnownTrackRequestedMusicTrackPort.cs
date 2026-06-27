using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Ports;

public interface ILoadKnownTrackRequestedMusicTrackPort
{
    Task<KnownTrackRequestedMusicTrack> LoadAsync(TrackId trackId, CancellationToken cancellationToken);
}
