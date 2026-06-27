using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;

public interface IPotentialCatalogLookupWorkStore
{
    Task<PotentialCatalogLookupWork?> FindByMusicCatalogIdAsync(MusicCatalogId musicCatalogId, CancellationToken cancellationToken);

    Task UpsertAsync(PotentialCatalogLookupWork candidate, CancellationToken cancellationToken);

    Task<IReadOnlyList<PotentialCatalogLookupWork>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken);
}
