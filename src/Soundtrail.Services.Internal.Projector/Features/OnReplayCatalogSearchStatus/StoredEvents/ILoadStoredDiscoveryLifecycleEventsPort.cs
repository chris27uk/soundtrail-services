using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.StoredEvents;

public interface ILoadStoredDiscoveryLifecycleEventsPort
{
    Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        LookupCriteria searchCriteria,
        CancellationToken cancellationToken);
}
