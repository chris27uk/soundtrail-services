using Soundtrail.Services.Enrichment.Shared.Search;

namespace Soundtrail.Services.Enrichment.Shared.Persistence;

public interface IRankedMusicCandidateStore
{
    Task<RankedMusicCandidate?> FindByMusicCatalogIdAsync(MusicCatalogId musicCatalogId, CancellationToken cancellationToken);

    Task UpsertAsync(RankedMusicCandidate candidate, CancellationToken cancellationToken);

    Task<IReadOnlyList<RankedMusicCandidate>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken);
}
