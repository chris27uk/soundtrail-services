using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class CatalogSearchTrackingStoreFake : ICatalogSearchTrackingStore
{
    private readonly Dictionary<string, CatalogSearchTracking> byCriteria = [];

    public IReadOnlyList<CatalogSearchTracking> All => byCriteria.Values.ToArray();

    public Task<CatalogSearchTracking?> FindByCriteriaAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        byCriteria.TryGetValue(criteria.Value, out var tracking);
        return Task.FromResult(tracking);
    }

    public Task UpsertAsync(
        CatalogSearchTracking tracking,
        CancellationToken cancellationToken)
    {
        byCriteria[tracking.Criteria.Value] = tracking;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CatalogSearchTracking>> GetByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CatalogSearchTracking> trackings = byCriteria.Values
            .Where(tracking => tracking.MusicCatalogId == musicCatalogId)
            .ToArray();
        return Task.FromResult(trackings);
    }

    public void Seed(CatalogSearchTracking tracking) => byCriteria[tracking.Criteria.Value] = tracking;
}
