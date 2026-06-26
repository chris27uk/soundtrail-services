using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public sealed class CatalogDiscoveryWork
{
    private readonly EventHandlers<CatalogDiscoveryWork> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private MusicCatalogId? musicCatalogId;
    private int requestCount;
    private int highestTrustLevelSeen;
    private int riskScore;
    private CatalogDiscoveryWorkStatus status;
    private DateTimeOffset? nextEligibleAt;
    private int version;

    private CatalogDiscoveryWork(
        MusicCatalogId musicCatalogId,
        IEnumerable<IDomainEvent> events,
        int version)
    {
        this.musicCatalogId = musicCatalogId;
        this.version = version;
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<CatalogDiscoveryWork> LoadAsync(
        ICatalogDiscoveryWorkRepository repository,
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(musicCatalogId, cancellationToken);
        return new CatalogDiscoveryWork(musicCatalogId, stream.Events, stream.Version);
    }

    public void RecordSearchRequested(CatalogSearchAttempt request)
    {
        Apply(
            new CatalogDiscoveryWorkRequested(
                RequireMusicCatalogId(),
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt),
            isNew: true);
    }

    public void Assess(
        ICatalogDiscoveryWorkPlanningPolicy policy,
        DateTimeOffset now)
    {
        var assessment = policy.Assess(
            new CatalogDiscoveryWorkSummary(
                RequireMusicCatalogId(),
                requestCount,
                highestTrustLevelSeen,
                riskScore,
                status,
                nextEligibleAt,
                Priority: null,
                Reason: null),
            now);

        switch (assessment.Action)
        {
            case CatalogDiscoveryWorkAction.Schedule:
                Apply(
                    new CatalogDiscoveryWorkScheduled(
                        RequireMusicCatalogId(),
                        assessment.Priority ?? throw new InvalidOperationException("Scheduled assessment must have a priority."),
                        now,
                        assessment.Reason,
                        now),
                    isNew: true);
                break;
            case CatalogDiscoveryWorkAction.Defer:
                Apply(
                    new CatalogDiscoveryWorkDeferred(
                        RequireMusicCatalogId(),
                        assessment.EarliestExpectedCompletionAt ?? now.AddSeconds(60),
                        assessment.Reason,
                        now),
                    isNew: true);
                break;
            case CatalogDiscoveryWorkAction.Ignore:
                Apply(
                    new CatalogDiscoveryWorkIgnored(
                        RequireMusicCatalogId(),
                        assessment.EarliestExpectedCompletionAt,
                        assessment.Reason,
                        now),
                    isNew: true);
                break;
        }
    }

    public async Task<bool> SaveAsync(
        ICatalogDiscoveryWorkRepository repository,
        CancellationToken cancellationToken)
    {
        if (uncommittedEvents.Count == 0)
        {
            return true;
        }

        var saved = await repository.AppendAsync(
            RequireMusicCatalogId(),
            version,
            uncommittedEvents.AsReadOnly(),
            cancellationToken);

        if (saved)
        {
            version += uncommittedEvents.Count;
            uncommittedEvents.Clear();
        }

        return saved;
    }

    private void Apply(IDomainEvent @event, bool isNew)
    {
        eventHandlers.Handle(@event);

        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
    }

    private MusicCatalogId RequireMusicCatalogId() =>
        musicCatalogId ?? throw new InvalidOperationException("Catalog discovery work music catalog id has not been established.");

    private EventHandlers<CatalogDiscoveryWork> CreateHandlers()
    {
        var handlers = new EventHandlers<CatalogDiscoveryWork>();
        handlers.Register<CatalogDiscoveryWorkRequested>(On);
        handlers.Register<CatalogDiscoveryWorkDeferred>(On);
        handlers.Register<CatalogDiscoveryWorkIgnored>(On);
        handlers.Register<CatalogDiscoveryWorkScheduled>(On);
        return handlers;
    }

    private void On(CatalogDiscoveryWorkRequested @event)
    {
        var nextStatus = ToStatus(Math.Max(riskScore, @event.RiskScore));
        musicCatalogId = @event.MusicCatalogId;
        requestCount += 1;
        highestTrustLevelSeen = Math.Max(highestTrustLevelSeen, @event.TrustLevel);
        riskScore = Math.Max(riskScore, @event.RiskScore);
        status = nextStatus;
        nextEligibleAt = nextStatus == CatalogDiscoveryWorkStatus.Ignored
            ? nextEligibleAt
            : nextEligibleAt;
    }

    private void On(CatalogDiscoveryWorkDeferred @event)
    {
        musicCatalogId = @event.MusicCatalogId;
        status = CatalogDiscoveryWorkStatus.Pending;
        nextEligibleAt = @event.NextEligibleAt;
    }

    private void On(CatalogDiscoveryWorkIgnored @event)
    {
        musicCatalogId = @event.MusicCatalogId;
        status = CatalogDiscoveryWorkStatus.Ignored;
        nextEligibleAt = @event.NextEligibleAt;
    }

    private void On(CatalogDiscoveryWorkScheduled @event)
    {
        musicCatalogId = @event.MusicCatalogId;
        status = CatalogDiscoveryWorkStatus.Pending;
        nextEligibleAt = null;
    }

    private static CatalogDiscoveryWorkStatus ToStatus(int riskScore) =>
        riskScore >= 90
            ? CatalogDiscoveryWorkStatus.Ignored
            : CatalogDiscoveryWorkStatus.Pending;
}
