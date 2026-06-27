using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class CatalogSearchTrackingStoreFake : ICatalogSearchTrackingStore
{
    private readonly Dictionary<string, CatalogSearchTracking> byCriteria = [];

    public IReadOnlyList<CatalogSearchTracking> All => byCriteria.Values.ToArray();

    public Task<CatalogSearchTracking?> FindByCriteriaAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        byCriteria.TryGetValue(ToPersistentId(searchCriteria), out var tracking);
        return Task.FromResult(tracking);
    }

    public Task UpsertAsync(
        CatalogSearchTracking tracking,
        CancellationToken cancellationToken)
    {
        byCriteria[ToPersistentId(tracking.SearchCriteria)] = tracking;
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

    public void Seed(CatalogSearchTracking tracking) => byCriteria[ToPersistentId(tracking.SearchCriteria)] = tracking;

    private static string ToPersistentId(MusicSearchCriteria searchCriteria) =>
        MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria);
}
