using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.ReplayDiscoveryLifecycleProjection.StoredEvents;

public interface ILoadStoredDiscoveryLifecycleEventsPort
{
    Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
