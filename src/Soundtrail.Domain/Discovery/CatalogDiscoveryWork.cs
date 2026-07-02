using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Events;

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
    private LookupPriorityBand? priority;
    private string? reason;
    private CatalogDiscoveryWork(
        MusicCatalogId musicCatalogId,
        IEnumerable<IDomainEvent> events)
    {
        this.musicCatalogId = musicCatalogId;
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<MusicCatalogId, IDomainEvent> Stream, CatalogDiscoveryWork Aggregate)> LoadAsync(
        IEventStreamRepository<MusicCatalogId, IDomainEvent> repository,
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(musicCatalogId, cancellationToken);
        return (stream, new CatalogDiscoveryWork(musicCatalogId, stream.Events));
    }

    public void CandidateIdentified(
        int trustLevel,
        int riskScore,
        DateTimeOffset requestedAt)
    {
        Apply(
            new CatalogDiscoveryWorkRequested(
                RequireMusicCatalogId(),
                trustLevel,
                riskScore,
                requestedAt),
            isNew: true);
    }

    public CatalogDiscoveryAssessmentResult Assess(
        ICatalogDiscoveryWorkPlanningPolicy policy,
        DateTimeOffset now,
        bool localTrackIsPlayable,
        int? trustLevel = null,
        int? riskScore = null)
    {
        if (trustLevel is not null && riskScore is not null)
        {
            CandidateIdentified(trustLevel.Value, riskScore.Value, now);
        }

        if (requestCount == 0)
        {
            return CatalogDiscoveryAssessmentResult.Noop();
        }

        var assessment = policy.Assess(ToSummary(), now);
        if (assessment.Action == CatalogDiscoveryWorkAction.Schedule && localTrackIsPlayable)
        {
            assessment = new CatalogDiscoveryWorkAssessment(
                CatalogDiscoveryWorkAction.Defer,
                Priority: null,
                EstimatedRetryAfterSeconds: 60,
                EarliestExpectedCompletionAt: now.AddSeconds(60),
                Reason: "Planner deferred lookup");
        }

        var changed = assessment.Action switch
        {
            CatalogDiscoveryWorkAction.Schedule => Schedule(assessment, now),
            CatalogDiscoveryWorkAction.Defer => Defer(assessment, now),
            CatalogDiscoveryWorkAction.Ignore => Ignore(assessment, now),
            _ => false
        };

        return new CatalogDiscoveryAssessmentResult(
            assessment.Action,
            assessment.Priority,
            assessment.EstimatedRetryAfterSeconds,
            assessment.EarliestExpectedCompletionAt,
            assessment.Reason);
    }

    public async Task SaveAsync(IEventStreamRepository<MusicCatalogId, IDomainEvent> repository,
        LoadedEventStream<MusicCatalogId, IDomainEvent> stream,
        OperationId? operationId,
        CancellationToken cancellationToken)
    {
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var saved = (await repository.AppendAsync(
            stream,
            uncommittedEvents.AsReadOnly(),
            operationId,
            cancellationToken)).Appended;

        if (saved)
        {
            uncommittedEvents.Clear();
        }
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

    private CatalogDiscoveryWorkSummary ToSummary() =>
        new(
            RequireMusicCatalogId(),
            requestCount,
            highestTrustLevelSeen,
            riskScore,
            status,
            nextEligibleAt,
            priority,
            reason);

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
        nextEligibleAt = null;
        priority = null;
        reason = null;
    }

    private void On(CatalogDiscoveryWorkDeferred @event)
    {
        musicCatalogId = @event.MusicCatalogId;
        status = CatalogDiscoveryWorkStatus.Pending;
        nextEligibleAt = @event.NextEligibleAt;
        priority = null;
        reason = @event.Reason;
    }

    private void On(CatalogDiscoveryWorkIgnored @event)
    {
        musicCatalogId = @event.MusicCatalogId;
        status = CatalogDiscoveryWorkStatus.Ignored;
        nextEligibleAt = @event.NextEligibleAt;
        priority = null;
        reason = @event.Reason;
    }

    private void On(CatalogDiscoveryWorkScheduled @event)
    {
        musicCatalogId = @event.MusicCatalogId;
        status = CatalogDiscoveryWorkStatus.Pending;
        nextEligibleAt = null;
        priority = @event.Priority;
        reason = @event.Reason;
    }

    private static CatalogDiscoveryWorkStatus ToStatus(int riskScore) =>
        riskScore >= 90
            ? CatalogDiscoveryWorkStatus.Ignored
            : CatalogDiscoveryWorkStatus.Pending;

    private bool Schedule(CatalogDiscoveryWorkAssessment assessment, DateTimeOffset now)
    {
        var scheduledPriority = assessment.Priority
                                ?? throw new InvalidOperationException("Scheduled assessment must have a priority.");
        var scheduledReason = assessment.Reason;

        if (status == CatalogDiscoveryWorkStatus.Pending
            && nextEligibleAt is null
            && priority == scheduledPriority
            && string.Equals(reason, scheduledReason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new CatalogDiscoveryWorkScheduled(
                RequireMusicCatalogId(),
                scheduledPriority,
                now,
                scheduledReason,
                now),
            isNew: true);
        return true;
    }

    private bool Defer(CatalogDiscoveryWorkAssessment assessment, DateTimeOffset now)
    {
        var deferredUntil = assessment.EarliestExpectedCompletionAt ?? now.AddSeconds(60);
        var deferredReason = assessment.Reason;

        if (status == CatalogDiscoveryWorkStatus.Pending
            && nextEligibleAt == deferredUntil
            && priority is null
            && string.Equals(reason, deferredReason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new CatalogDiscoveryWorkDeferred(
                RequireMusicCatalogId(),
                deferredUntil,
                deferredReason,
                now),
            isNew: true);
        return true;
    }

    private bool Ignore(CatalogDiscoveryWorkAssessment assessment, DateTimeOffset now)
    {
        var ignoredUntil = assessment.EarliestExpectedCompletionAt;
        var ignoredReason = assessment.Reason;

        if (status == CatalogDiscoveryWorkStatus.Ignored
            && nextEligibleAt == ignoredUntil
            && priority is null
            && string.Equals(reason, ignoredReason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new CatalogDiscoveryWorkIgnored(
                RequireMusicCatalogId(),
                ignoredUntil,
                ignoredReason,
                now),
            isNew: true);
        return true;
    }
}
