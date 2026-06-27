using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Raven.CatalogDiscoveryWork;

public sealed class RavenCatalogDiscoveryWorkRepository(
    IDocumentStore documentStore,
    IAsyncDocumentSession? session = null) : ICatalogDiscoveryWorkRepository, ICatalogDiscoveryWorkPlanningReadPort
{
    async Task<CatalogDiscoveryWorkSummary?> ICatalogDiscoveryWorkPlanningReadPort.LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var document = await activeSession.LoadAsync<CatalogDiscoveryWorkSummaryRecordDto>(
                CatalogDiscoveryWorkSummaryRecordDto.GetDocumentId(musicCatalogId.Value),
                cancellationToken);

            return document is null
                ? null
                : CatalogDiscoveryWorkEventRecordMapper.ToSummary(document);
        }
    }

    public async Task<CatalogDiscoveryWorkEventStream> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var metadata = await activeSession.LoadAsync<CatalogDiscoveryWorkEventStreamMetadataRecordDto>(
                CatalogDiscoveryWorkEventStreamMetadataRecordDto.GetDocumentId(musicCatalogId.Value),
                cancellationToken);

            if (metadata is null)
            {
                return new CatalogDiscoveryWorkEventStream(0, []);
            }

            var storedEvents = (await activeSession.Advanced.LoadStartingWithAsync<CatalogDiscoveryWorkStoredEventRecordDto>(
                    $"catalog-discovery-work-events/{musicCatalogId.Value}/"))
                .OrderBy(x => x.Version)
                .ToList();

            return new CatalogDiscoveryWorkEventStream(
                metadata.Version,
                storedEvents.Select(CatalogDiscoveryWorkEventRecordMapper.ToDomainEvent).ToArray());
        }
    }

    public async Task<bool> AppendAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        IReadOnlyCollection<Soundtrail.Domain.Events.IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return true;
        }

        var ownsSession = session is null;
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            activeSession.Advanced.UseOptimisticConcurrency = true;

            var metadataId = CatalogDiscoveryWorkEventStreamMetadataRecordDto.GetDocumentId(musicCatalogId.Value);
            var metadata = await activeSession.LoadAsync<CatalogDiscoveryWorkEventStreamMetadataRecordDto>(metadataId, cancellationToken)
                ?? new CatalogDiscoveryWorkEventStreamMetadataRecordDto
                {
                    Id = metadataId,
                    MusicCatalogId = musicCatalogId.Value
                };

            if (metadata.Version != expectedVersion)
            {
                return false;
            }

            var startingVersion = metadata.Version;
            metadata.Version += events.Count;
            metadata.UpdatedAtUtc = events.Max(CatalogDiscoveryWorkEventRecordMapper.GetOccurredAtUtc);
            await activeSession.StoreAsync(metadata, cancellationToken);

            foreach (var storedEvent in CatalogDiscoveryWorkEventRecordMapper.ToStoredEvents(musicCatalogId, events, startingVersion))
            {
                await activeSession.StoreAsync(storedEvent, cancellationToken);
            }

            var summaryId = CatalogDiscoveryWorkSummaryRecordDto.GetDocumentId(musicCatalogId.Value);
            var summary = await activeSession.LoadAsync<CatalogDiscoveryWorkSummaryRecordDto>(summaryId, cancellationToken)
                ?? new CatalogDiscoveryWorkSummaryRecordDto
                {
                    Id = summaryId,
                    MusicCatalogId = musicCatalogId.Value
                };
            CatalogDiscoveryWorkEventRecordMapper.Apply(summary, events);
            await activeSession.StoreAsync(summary, cancellationToken);

            if (ownsSession)
            {
                await activeSession.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
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
                .Select(CatalogDiscoveryWorkEventRecordMapper.ToSummary)
                .ToArray();
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
