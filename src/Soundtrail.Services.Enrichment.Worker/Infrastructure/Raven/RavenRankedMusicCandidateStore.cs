using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Documents;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Indexes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;

public sealed class RavenRankedMusicCandidateStore(IDocumentStore documentStore) : IRankedMusicCandidateStore
{
    public async Task<RankedMusicCandidate?> FindByMusicCatalogIdAsync(MusicCatalogId musicCatalogId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var document = await session.LoadAsync<RavenRankedMusicCandidateDocument>(
            RavenRankedMusicCandidateDocument.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        return document?.ToDomain();
    }

    public async Task UpsertAsync(RankedMusicCandidate candidate, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(candidate.ToDocument(), cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RankedMusicCandidate>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var documents = await session
            .Query<RavenRankedMusicCandidateDocument, RankedMusicCandidates_ByPlanning>()
            .Where(candidate => candidate.Status == RankedMusicCandidateStatus.Pending.ToString())
            .Where(candidate => candidate.NextEligibleAt == null || candidate.NextEligibleAt <= now)
            .OrderByDescending(candidate => candidate.HighestTrustLevelSeen)
            .ThenByDescending(candidate => candidate.RequestCount)
            .Take(take)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToArray();
    }
}
