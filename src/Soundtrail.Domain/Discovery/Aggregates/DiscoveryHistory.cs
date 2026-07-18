using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Domain.Discovery.Aggregates;

public sealed class DiscoveryHistory
{
    private readonly EventHandlers eventHandlers = new();
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private readonly HashSet<string> requestedTargets = [];
    private readonly HashSet<string> scheduledTargets = [];
    private readonly HashSet<string> completedTargets = [];
    private readonly HashSet<string> rejectedTargets = [];
    private readonly HashSet<string> ignoredTargets = [];
    private readonly LoadedEventStream<CatalogWorkId> stream;
    private readonly IEventStreamRepository<CatalogWorkId> repository;
    private readonly SearchRequestContext requestContext;

    private DiscoveryHistory(
        LoadedEventStream<CatalogWorkId> stream, 
        IEventStreamRepository<CatalogWorkId> repository, 
        SearchRequestContext requestContext)
    {
        this.stream = stream;
        this.repository = repository;
        this.requestContext = requestContext;
        this.eventHandlers.Register<WorkRequested>(@event => requestedTargets.Add(@event.Target.NormalisedIdentifier));
        this.eventHandlers.Register<WorkScheduled>(@event => scheduledTargets.Add(@event.Target.NormalisedIdentifier));
        this.eventHandlers.Register<WorkDeferred>(_ => { });
        this.eventHandlers.Register<WorkCompleted>(@event => completedTargets.Add(@event.Target.NormalisedIdentifier));
        this.eventHandlers.Register<WorkRejected>(@event => rejectedTargets.Add(@event.Target.NormalisedIdentifier));
        this.eventHandlers.Register<WorkIgnored>(@event => ignoredTargets.Add(@event.Target.NormalisedIdentifier));
        foreach (var @event in stream.Events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<CatalogWorkId> Stream, DiscoveryHistory Aggregate)> LoadAsync(
        IEventStreamRepository<CatalogWorkId> repository,
        CatalogWorkId streamId,
        SearchRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(streamId, cancellationToken);
        return (stream, new DiscoveryHistory(stream, repository, requestContext));
    }

    public void Request(IEnumerable<EnrichmentTarget> operations, LookupPriorityBand priority)
    {
        foreach (var operation in operations)
        {
            RequestWork(operation, priority);
        }
    }

    public PlanningAssessmentFlow Assess(PlanningAssessment assessment) => new(this, assessment);

    private void Schedule(
        EnrichmentTarget target,
        LookupPriorityBand priority,
        DateTimeOffset nextEligibleAt,
        DateTimeOffset earliestExpectedCompletionAt,
        string reason)
    {
        Apply(new WorkScheduled(
            target,
            priority,
            nextEligibleAt,
            earliestExpectedCompletionAt,
            reason,
            requestContext.RequestedAt), isNew: true);
    }

    private void Defer(
        EnrichmentTarget target,
        LookupPriorityBand priority,
        DateTimeOffset nextEligibleAt,
        string reason)
    {
        Apply(new WorkDeferred(
            target,
            priority,
            nextEligibleAt,
            EstimateRetryAfterSeconds(nextEligibleAt),
            reason,
            requestContext.RequestedAt), isNew: true);
    }

    private void Ignore(
        EnrichmentTarget target,
        LookupPriorityBand priority,
        DateTimeOffset? nextEligibleAt,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason)
    {
        Apply(new WorkIgnored(
            target,
            priority,
            nextEligibleAt,
            nextEligibleAt is null ? null : EstimateRetryAfterSeconds(nextEligibleAt.Value),
            earliestExpectedCompletionAt,
            reason,
            requestContext.RequestedAt), isNew: true);
    }

    private void Reject(
        EnrichmentTarget target,
        LookupPriorityBand priority,
        string reason)
    {
        Apply(new WorkRejected(target, priority, reason, requestContext.RequestedAt), isNew: true);
    }
    
    private void RequestWork(EnrichmentTarget operation, LookupPriorityBand priority)
    {
        Apply(
            new WorkRequested(
                operation,
                priority,
                this.requestContext.TrustLevel,
                this.requestContext.RiskScore,
                this.requestContext.RequestedAt,
                this.requestContext.CorrelationId),
            isNew: true);
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        var append = await this.repository.AppendAsync(
            this.stream,
            uncommittedEvents.AsReadOnly(),
            OperationId.From(this.requestContext.CorrelationId),
            cancellationToken);

        if (append.Outcome == AppendOutcome.VersionMismatch)
        {
            throw new InvalidOperationException($"Catalog search stream concurrency conflict for '{stream.StreamId.StableValue}'.");
        }

        if (append.Appended || append.Outcome == AppendOutcome.DuplicateOperation)
        {
            this.uncommittedEvents.Clear();
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

    private int EstimateRetryAfterSeconds(DateTimeOffset nextEligibleAt)
    {
        var delay = nextEligibleAt - requestContext.RequestedAt;
        return Math.Max(0, (int)Math.Ceiling(delay.TotalSeconds));
    }

    public sealed record SearchRequestContext(
        int TrustLevel,
        int RiskScore,
        DateTimeOffset RequestedAt,
        CorrelationId CorrelationId);

    public sealed class PlanningAssessmentFlow
    {
        private readonly DiscoveryHistory aggregate;
        private readonly PlanningAssessment assessment;
        private bool decided;

        internal PlanningAssessmentFlow(
            DiscoveryHistory aggregate,
            PlanningAssessment assessment)
        {
            this.aggregate = aggregate;
            this.assessment = assessment;
        }

        public PlanningAssessmentFlow IgnoreCompletedWork()
        {
            if (!decided && aggregate.completedTargets.Contains(assessment.Target.NormalisedIdentifier))
            {
                aggregate.Ignore(
                    assessment.Target,
                    assessment.Priority,
                    nextEligibleAt: null,
                    earliestExpectedCompletionAt: null,
                    "Work already completed.");
                decided = true;
            }

            return this;
        }

        public PlanningAssessmentFlow RejectPreviouslyRejectedWork()
        {
            if (!decided && aggregate.rejectedTargets.Contains(assessment.Target.NormalisedIdentifier))
            {
                aggregate.Reject(
                    assessment.Target,
                    assessment.Priority,
                    "Work was previously rejected.");
                decided = true;
            }

            return this;
        }

        public PlanningAssessmentFlow IgnoreDuplicateWork()
        {
            if (!decided && (aggregate.scheduledTargets.Contains(assessment.Target.NormalisedIdentifier) || assessment.Projection.HasEquivalentWorkInFlight))
            {
                aggregate.Ignore(
                    assessment.Target,
                    assessment.Priority,
                    nextEligibleAt: null,
                    earliestExpectedCompletionAt: assessment.Projection.EquivalentWorkExpectedCompletionAt,
                    "Equivalent work is already planned.");
                decided = true;
            }

            return this;
        }

        public PlanningAssessmentFlow DeferWhenHighPriorityCapacityIsProtected()
        {
            if (!decided && assessment.HighPriorityCapacityIsProtected)
            {
                aggregate.Defer(
                    assessment.Target,
                    assessment.Priority,
                    assessment.DeferredUntil,
                    "Capacity is reserved for higher-priority discovery work.");
                decided = true;
            }

            return this;
        }

        public PlanningAssessmentFlow DeferWhenPlannerCapacityIsFull()
        {
            if (!decided && assessment.PlannerCapacityIsFull)
            {
                aggregate.Defer(
                    assessment.Target,
                    assessment.Priority,
                    assessment.DeferredUntil,
                    "Planner capacity is currently full.");
                decided = true;
            }

            return this;
        }

        public void ScheduleOtherwise()
        {
            if (decided)
            {
                return;
            }

            aggregate.Schedule(
                assessment.Target,
                assessment.Priority,
                assessment.RequestedAt,
                assessment.ExpectedCompletionAt,
                "Work is valuable and within coarse planner capacity.");
        }
    }
}
