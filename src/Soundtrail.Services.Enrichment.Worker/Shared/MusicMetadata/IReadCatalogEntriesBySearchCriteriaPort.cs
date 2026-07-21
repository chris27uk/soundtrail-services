using Soundtrail.Domain.Search;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

public interface IReadCatalogEntriesBySearchCriteriaPort
{
    Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(
        SearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
