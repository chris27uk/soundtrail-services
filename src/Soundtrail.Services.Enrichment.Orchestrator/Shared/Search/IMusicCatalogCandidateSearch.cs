using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

public interface IMusicCatalogCandidateSearch
{
    Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(LookupCriteria searchCriteria, CancellationToken cancellationToken);
}
