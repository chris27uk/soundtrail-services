using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;

public interface ISaveDiscoveryLifecycleProjectionPort
{
    Task SaveAsync(
        DiscoveryLifecycleProjection projection,
        CancellationToken cancellationToken);
}
