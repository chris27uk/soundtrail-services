using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayDiscoveryLifecycleProjection.StoredEvents;

public interface ILoadStoredDiscoveryLifecycleEventsPort
{
    Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
