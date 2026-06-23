using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.ProjectDiscoveryLifecycle.Ports;

public interface ILoadDiscoveryLifecycleProjectionPort
{
    Task<DiscoveryLifecycleProjection> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
