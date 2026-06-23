using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.ProjectDiscoveryLifecycle.Ports;

public interface ISaveDiscoveryLifecycleProjectionPort
{
    Task SaveAsync(
        DiscoveryLifecycleProjection projection,
        CancellationToken cancellationToken);
}
