using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters.Documents;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters.Indexes;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters;

public sealed class RavenPotentialCatalogLookupWorkStore(
    IDocumentStore documentStore,
    IAsyncDocumentSession? session = null) : IPotentialCatalogLookupWorkStore
{
    public async Task<PotentialCatalogLookupWork?> FindByMusicCatalogIdAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var document = await activeSession.LoadAsync<RavenPotentialCatalogLookupWorkRecordDto>(
                RavenPotentialCatalogLookupWorkRecordDto.GetDocumentId(musicCatalogId.Value),
                cancellationToken);

            return document?.ToDomain();
        }
    }

    public async Task UpsertAsync(PotentialCatalogLookupWork candidate, CancellationToken cancellationToken)
    {
        var ownsSession = session is null;
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var documentId = RavenPotentialCatalogLookupWorkRecordDto.GetDocumentId(candidate.MusicCatalogId.Value);
            var existing = await activeSession.LoadAsync<RavenPotentialCatalogLookupWorkRecordDto>(
                documentId,
                cancellationToken);

            if (existing is null)
            {
                await activeSession.StoreAsync(candidate.ToRecordDto(), cancellationToken);
            }
            else
            {
                candidate.ApplyTo(existing);
            }

            if (ownsSession)
            {
                await activeSession.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<PotentialCatalogLookupWork>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var documents = await activeSession
                .Query<RavenPotentialCatalogLookupWorkRecordDto, PotentialCatalogLookupWork_ByPlanning>()
                .Where(candidate => candidate.Status == nameof(PotentialCatalogLookupWorkStatus.Pending))
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
