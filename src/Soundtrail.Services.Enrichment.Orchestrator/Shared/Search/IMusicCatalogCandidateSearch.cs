using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

public interface IMusicCatalogCandidateSearch
{
    Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(MusicSearchCriteria searchCriteria, CancellationToken cancellationToken);
}
