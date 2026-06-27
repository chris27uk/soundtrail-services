using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Discovery;

public interface ICatalogSearchTrackingStore
{
    Task<CatalogSearchTracking?> FindByCriteriaAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        CatalogSearchTracking tracking,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogSearchTracking>> GetByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
