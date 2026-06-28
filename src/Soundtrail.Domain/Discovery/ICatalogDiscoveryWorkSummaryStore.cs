using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public interface ICatalogDiscoveryWorkSummaryStore
{
    Task<CatalogDiscoveryWorkSummarySnapshot?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);

    Task SaveAsync(
        CatalogDiscoveryWorkSummarySnapshot snapshot,
        CancellationToken cancellationToken);
}
