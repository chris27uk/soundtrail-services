namespace Soundtrail.Domain.Discovery;

public interface ILoadDiscoveryLifecycleProjectionPort
{
    Task<DiscoveryLifecycleProjection> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
