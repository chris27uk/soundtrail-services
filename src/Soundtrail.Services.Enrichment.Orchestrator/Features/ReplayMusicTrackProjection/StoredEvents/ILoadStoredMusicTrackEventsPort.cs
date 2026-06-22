using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayMusicTrackProjection.StoredEvents;

public interface ILoadStoredMusicTrackEventsPort
{
    Task<IReadOnlyList<VersionedMusicTrackEvent>> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
