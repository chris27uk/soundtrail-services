using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;

public interface IRankedMusicCandidateStore
{
    Task<RankedMusicCandidate?> FindByMusicCatalogIdAsync(MusicCatalogId musicCatalogId, CancellationToken cancellationToken);

    Task UpsertAsync(RankedMusicCandidate candidate, CancellationToken cancellationToken);

    Task<IReadOnlyList<RankedMusicCandidate>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken);
}
