using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Raven.CatalogDiscoveryWork;

public sealed class RavenCatalogDiscoveryWorkSummaryStore(
    IDocumentStore documentStore,
    IAsyncDocumentSession? session = null) : ICatalogDiscoveryWorkSummaryStore, ICatalogDiscoveryWorkPlanningReadPort
{
    public async Task<CatalogDiscoveryWorkSummarySnapshot?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var document = await activeSession.LoadAsync<CatalogDiscoveryWorkSummaryRecordDto>(
                CatalogDiscoveryWorkSummaryRecordDto.GetDocumentId(musicCatalogId.Value),
                cancellationToken);

            return document is null ? null : ToSnapshot(document);
        }
    }

    public async Task SaveAsync(
        CatalogDiscoveryWorkSummarySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var ownsSession = session is null;
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var documentId = CatalogDiscoveryWorkSummaryRecordDto.GetDocumentId(snapshot.MusicCatalogId.Value);
            var document = await activeSession.LoadAsync<CatalogDiscoveryWorkSummaryRecordDto>(documentId, cancellationToken)
                ?? new CatalogDiscoveryWorkSummaryRecordDto
                {
                    Id = documentId,
                    MusicCatalogId = snapshot.MusicCatalogId.Value
                };

            Apply(snapshot, document);
            await activeSession.StoreAsync(document, cancellationToken);

            if (ownsSession)
            {
                await activeSession.SaveChangesAsync(cancellationToken);
            }
        }
    }

    async Task<CatalogDiscoveryWorkSummary?> ICatalogDiscoveryWorkPlanningReadPort.LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var snapshot = await LoadAsync(musicCatalogId, cancellationToken);
        return snapshot is null ? null : CatalogDiscoveryWorkSummaryProjection.Load(snapshot).ToSummary();
    }

    public async Task<IReadOnlyList<CatalogDiscoveryWorkSummary>> GetPlanningCandidatesAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var documents = await activeSession
                .Query<CatalogDiscoveryWorkSummaryRecordDto>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(candidate => candidate.Status == nameof(CatalogDiscoveryWorkStatus.Pending))
                .Where(candidate => candidate.NextEligibleAt == null || candidate.NextEligibleAt <= now)
                .OrderByDescending(candidate => candidate.HighestTrustLevelSeen)
                .ThenByDescending(candidate => candidate.RequestCount)
                .Take(take)
                .ToListAsync(cancellationToken);

            return documents
                .Select(document => CatalogDiscoveryWorkSummaryProjection.Load(ToSnapshot(document)).ToSummary())
                .ToArray();
        }
    }

    private static CatalogDiscoveryWorkSummarySnapshot ToSnapshot(CatalogDiscoveryWorkSummaryRecordDto document) =>
        new(
            MusicCatalogId.From(document.MusicCatalogId),
            document.RequestCount,
            document.HighestTrustLevelSeen,
            document.RiskScore,
            Enum.Parse<CatalogDiscoveryWorkStatus>(document.Status, ignoreCase: true),
            document.NextEligibleAt,
            document.Priority is null ? null : Enum.Parse<LookupPriorityBand>(document.Priority, ignoreCase: true),
            document.Reason,
            document.UpdatedAtUtc,
            document.LastAppliedVersion);

    private static void Apply(
        CatalogDiscoveryWorkSummarySnapshot snapshot,
        CatalogDiscoveryWorkSummaryRecordDto document)
    {
        document.Id = CatalogDiscoveryWorkSummaryRecordDto.GetDocumentId(snapshot.MusicCatalogId.Value);
        document.MusicCatalogId = snapshot.MusicCatalogId.Value;
        document.RequestCount = snapshot.RequestCount;
        document.HighestTrustLevelSeen = snapshot.HighestTrustLevelSeen;
        document.RiskScore = snapshot.RiskScore;
        document.Status = snapshot.Status.ToString();
        document.NextEligibleAt = snapshot.NextEligibleAt;
        document.Priority = snapshot.Priority?.ToString();
        document.Reason = snapshot.Reason;
        document.UpdatedAtUtc = snapshot.UpdatedAt;
        document.LastAppliedVersion = snapshot.LastAppliedVersion;
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
