using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;

public interface IRankedMusicCandidateStore
{
    Task<RankedMusicCandidate?> FindByMusicCatalogIdAsync(MusicCatalogId musicCatalogId, CancellationToken cancellationToken);

    Task UpsertAsync(RankedMusicCandidate candidate, CancellationToken cancellationToken);

    Task<IReadOnlyList<RankedMusicCandidate>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken);
}
