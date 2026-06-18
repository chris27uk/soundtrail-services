using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public interface ICatalogSearchTrackingStore
{
    Task<CatalogSearchTracking?> FindByCriteriaAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        CatalogSearchTracking tracking,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogSearchTracking>> GetByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
