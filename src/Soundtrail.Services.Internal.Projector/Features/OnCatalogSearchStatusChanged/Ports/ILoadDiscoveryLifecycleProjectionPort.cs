using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;

public interface ILoadDiscoveryLifecycleProjectionPort
{
    Task<DiscoveryLifecycleProjection> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
