using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.StoredEvents;

public interface ILoadStoredMusicTrackEventsPort
{
    Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
