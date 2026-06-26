using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public interface ICatalogDiscoveryWorkPlanningReadPort
{
    Task<CatalogDiscoveryWorkSummary?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogDiscoveryWorkSummary>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken);
}
