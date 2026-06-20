using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Catalog.Projector.Features.ReplayMusicTrackCatalogProjection.StoredEvents;

public interface ILoadStoredMusicTrackEventsPort
{
    Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
