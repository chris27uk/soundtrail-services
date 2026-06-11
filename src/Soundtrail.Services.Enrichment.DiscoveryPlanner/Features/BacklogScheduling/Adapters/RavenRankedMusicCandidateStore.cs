using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters.Documents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters.Indexes;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;

public sealed class RavenRankedMusicCandidateStore(
    IDocumentStore documentStore,
    IAsyncDocumentSession? session = null) : IRankedMusicCandidateStore
{
    public async Task<RankedMusicCandidate?> FindByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var document = await activeSession.LoadAsync<RavenRankedMusicCandidateDocument>(
                RavenRankedMusicCandidateDocument.GetDocumentId(musicCatalogId.Value),
                cancellationToken);

            return document?.ToDomain();
        }
    }

    public async Task UpsertAsync(RankedMusicCandidate candidate, CancellationToken cancellationToken)
    {
        var ownsSession = session is null;
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            await activeSession.StoreAsync(candidate.ToDocument(), cancellationToken);
            if (ownsSession)
            {
                await activeSession.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<RankedMusicCandidate>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var documents = await activeSession
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

    private (IAsyncDocumentSession Session, IDisposable Dispose) OpenSession()
    {
        if (session is not null)
        {
            return (session, NoopDisposable.Instance);
        }

        var openedSession = documentStore.OpenAsyncSession();
        return (openedSession, openedSession);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
