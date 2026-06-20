namespace Soundtrail.Domain.Discovery;

public interface ISaveDiscoveryLifecycleProjectionPort
{
    Task SaveAsync(
        DiscoveryLifecycleProjection projection,
        CancellationToken cancellationToken);
}
