using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.StoredEvents;

public interface ILoadStoredDiscoveryLifecycleEventsPort
{
    Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
