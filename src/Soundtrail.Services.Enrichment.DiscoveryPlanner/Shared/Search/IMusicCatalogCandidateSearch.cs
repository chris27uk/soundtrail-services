using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

public interface IMusicCatalogCandidateSearch
{
    Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(NormalizedSearchQuery query, CancellationToken cancellationToken);
}
