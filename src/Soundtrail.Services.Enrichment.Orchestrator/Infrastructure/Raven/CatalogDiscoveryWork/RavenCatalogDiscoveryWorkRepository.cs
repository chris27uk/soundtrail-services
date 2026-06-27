using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Adapters.Discovery;

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
                : ToSummary(document);
        }
    }

    public async Task<CatalogDiscoveryWorkEventStream> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var stream = await CreateEventStore(activeSession).LoadAsync(musicCatalogId, cancellationToken);
            return new CatalogDiscoveryWorkEventStream(stream.Version, stream.Events);
        }
    }

    public async Task<bool> AppendAsync(
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        IReadOnlyCollection<IDomainEvent> events,
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
            var append = await CreateEventStore(activeSession).AppendAsync(
                new AppendRequest<MusicCatalogId, IDomainEvent>(musicCatalogId, expectedVersion, events.ToArray()),
                cancellationToken,
                saveChanges: ownsSession,
                beforeSave: async (appendSession, request, token) =>
                {
                    var summaryId = CatalogDiscoveryWorkSummaryRecordDto.GetDocumentId(request.StreamId.Value);
                    var summary = await appendSession.LoadAsync<CatalogDiscoveryWorkSummaryRecordDto>(summaryId, token)
                        ?? new CatalogDiscoveryWorkSummaryRecordDto
                        {
                            Id = summaryId,
                            MusicCatalogId = request.StreamId.Value
                        };
                    Apply(summary, request.Events);
                    await appendSession.StoreAsync(summary, token);
                });

            return append.Appended;
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
                .Select(ToSummary)
                .ToArray();
        }
    }

    private static CatalogDiscoveryWorkSummary ToSummary(CatalogDiscoveryWorkSummaryRecordDto document) =>
        new(
            MusicCatalogId.From(document.MusicCatalogId),
            document.RequestCount,
            document.HighestTrustLevelSeen,
            document.RiskScore,
            Enum.Parse<CatalogDiscoveryWorkStatus>(document.Status, ignoreCase: true),
            document.NextEligibleAt,
            document.Priority is null ? null : Enum.Parse<LookupPriorityBand>(document.Priority, ignoreCase: true),
            document.Reason);

    private static void Apply(
        CatalogDiscoveryWorkSummaryRecordDto document,
        IReadOnlyCollection<IDomainEvent> events)
    {
        foreach (var @event in events)
        {
            switch (@event)
            {
                case CatalogDiscoveryWorkRequested requested:
                    document.MusicCatalogId = requested.MusicCatalogId.Value;
                    document.RequestCount += 1;
                    document.HighestTrustLevelSeen = Math.Max(document.HighestTrustLevelSeen, requested.TrustLevel);
                    document.RiskScore = Math.Max(document.RiskScore, requested.RiskScore);
                    document.Status = (requested.RiskScore >= 90
                        ? CatalogDiscoveryWorkStatus.Ignored
                        : CatalogDiscoveryWorkStatus.Pending).ToString();
                    document.Reason = null;
                    document.UpdatedAtUtc = requested.RequestedAt;
                    break;
                case CatalogDiscoveryWorkDeferred deferred:
                    document.MusicCatalogId = deferred.MusicCatalogId.Value;
                    document.Status = CatalogDiscoveryWorkStatus.Pending.ToString();
                    document.NextEligibleAt = deferred.NextEligibleAt;
                    document.Priority = null;
                    document.Reason = deferred.Reason;
                    document.UpdatedAtUtc = deferred.DeferredAt;
                    break;
                case CatalogDiscoveryWorkIgnored ignored:
                    document.MusicCatalogId = ignored.MusicCatalogId.Value;
                    document.Status = CatalogDiscoveryWorkStatus.Ignored.ToString();
                    document.NextEligibleAt = ignored.NextEligibleAt;
                    document.Priority = null;
                    document.Reason = ignored.Reason;
                    document.UpdatedAtUtc = ignored.IgnoredAt;
                    break;
                case CatalogDiscoveryWorkScheduled scheduled:
                    document.MusicCatalogId = scheduled.MusicCatalogId.Value;
                    document.Status = CatalogDiscoveryWorkStatus.Pending.ToString();
                    document.NextEligibleAt = null;
                    document.Priority = scheduled.Priority.ToString();
                    document.Reason = scheduled.Reason;
                    document.UpdatedAtUtc = scheduled.ScheduledAt;
                    break;
            }
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

    private static RavenEventStore<MusicCatalogId, IDomainEvent, CatalogDiscoveryWorkStoredEventRecordDto, CatalogDiscoveryWorkEventStreamMetadataRecordDto> CreateEventStore(
        IAsyncDocumentSession session) =>
        new(
            session,
            streamId => CatalogDiscoveryWorkEventStreamMetadataRecordDto.GetDocumentId(streamId.StableValue),
            (streamId, metadataId) => new CatalogDiscoveryWorkEventStreamMetadataRecordDto
            {
                Id = metadataId,
                MusicCatalogId = streamId.StableValue
            },
            streamId => $"catalog-discovery-work-events/{streamId.StableValue}/",
            (streamId, version, _, @event) => CatalogDiscoveryWorkStoredEventTranslator.ToStoredEvent(streamId, @event, version),
            CatalogDiscoveryWorkStoredEventTranslator.ToEvent,
            storedEvent => storedEvent.OccurredAtUtc,
            storedEvent => storedEvent.Version);
}
