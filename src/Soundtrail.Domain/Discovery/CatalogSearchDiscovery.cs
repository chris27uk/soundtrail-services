using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public sealed class CatalogSearchDiscovery
{
    private readonly EventHandlers<CatalogSearchDiscovery> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private CatalogSearchCriteria? criteria;
    private CatalogSearchLifecycleStatus? status;
    private LookupPriorityBand? priority;
    private bool willBeLookedUp;
    private int? estimatedRetryAfterSeconds;
    private DateTimeOffset? earliestExpectedCompletionAt;
    private string? reason;
    private DateTimeOffset? updatedAt;
    private bool hasRequested;
    private int version;

    private CatalogSearchDiscovery(IEnumerable<IDomainEvent> events, int version)
    {
        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }

        this.eventHandlers = CreateHandlers();
        this.version = version;
    }

    public static async Task<CatalogSearchDiscovery> LoadAsync(ICatalogSearchDiscoveryRepository repository, CatalogSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(criteria, cancellationToken);
        var discovery = new CatalogSearchDiscovery(stream.Events, stream.Version);
        discovery.criteria ??= criteria;
        return discovery;
    }

    public bool Request(CatalogSearchAttempt request)
    {
        if (this.hasRequested)
        {
            return false;
        }

        Apply(
            new DiscoveryRequested(
                request.Criteria,
                request.Query,
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt,
                request.CorrelationId),
            isNew: true);

        return true;
    }

    public bool Plan(
        LookupPriorityBand priority,
        int? estimatedRetryAfterSeconds,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        DateTimeOffset plannedAt)
    {
        EnsureCanTransitionTo(CatalogSearchLifecycleStatus.Planned);

        if (status == CatalogSearchLifecycleStatus.Planned
            && this.priority == priority
            && willBeLookedUp
            && this.estimatedRetryAfterSeconds == estimatedRetryAfterSeconds
            && this.earliestExpectedCompletionAt == earliestExpectedCompletionAt
            && string.Equals(this.reason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new DiscoveryPlanned(
                RequireCriteria(),
                priority,
                WillBeLookedUp: true,
                estimatedRetryAfterSeconds,
                earliestExpectedCompletionAt,
                reason,
                plannedAt),
            isNew: true);

        return true;
    }

    public bool Defer(
        int? estimatedRetryAfterSeconds,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        DateTimeOffset deferredAt)
    {
        EnsureCanTransitionTo(CatalogSearchLifecycleStatus.Deferred);

        if (status == CatalogSearchLifecycleStatus.Deferred
            && willBeLookedUp
            && this.estimatedRetryAfterSeconds == estimatedRetryAfterSeconds
            && this.earliestExpectedCompletionAt == earliestExpectedCompletionAt
            && string.Equals(this.reason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new DiscoveryDeferred(
                RequireCriteria(),
                WillBeLookedUp: true,
                estimatedRetryAfterSeconds,
                earliestExpectedCompletionAt,
                reason,
                deferredAt),
            isNew: true);

        return true;
    }

    public bool Reject(string reason, DateTimeOffset rejectedAt)
    {
        EnsureCanTransitionTo(CatalogSearchLifecycleStatus.Rejected);

        if (status == CatalogSearchLifecycleStatus.Rejected
            && !willBeLookedUp
            && string.Equals(this.reason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new DiscoveryRejected(
                RequireCriteria(),
                WillBeLookedUp: false,
                reason,
                rejectedAt),
            isNew: true);

        return true;
    }

    public bool Fail(string reason, DateTimeOffset failedAt)
    {
        EnsureCanTransitionTo(CatalogSearchLifecycleStatus.Failed);

        if (status == CatalogSearchLifecycleStatus.Failed
            && !willBeLookedUp
            && string.Equals(this.reason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new DiscoveryFailed(
                RequireCriteria(),
                WillBeLookedUp: false,
                reason,
                failedAt),
            isNew: true);

        return true;
    }

    public async Task<bool> SaveAsync(
        ICatalogSearchDiscoveryRepository repository,
        CancellationToken cancellationToken)
    {
        if (uncommittedEvents.Count == 0)
        {
            return true;
        }

        var saved = await repository.AppendAsync(
            RequireCriteria(),
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
        this.eventHandlers.Handle(@event);

        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
    }

    private CatalogSearchCriteria RequireCriteria() =>
        criteria ?? throw new InvalidOperationException("Discovery criteria has not been established.");

    private void EnsureCanTransitionTo(CatalogSearchLifecycleStatus targetStatus)
    {
        if (status is null)
        {
            return;
        }

        if (status == targetStatus)
        {
            return;
        }

        if (status == CatalogSearchLifecycleStatus.Rejected)
        {
            throw new InvalidOperationException("Rejected discovery cannot transition to another state.");
        }
    }

    private EventHandlers<CatalogSearchDiscovery> CreateHandlers()
    {
        var handlers = new EventHandlers<CatalogSearchDiscovery>();
        handlers.Register<DiscoveryRequested>(this.On);
        handlers.Register<DiscoveryPlanned>(this.On);
        handlers.Register<DiscoveryDeferred>(this.On);
        handlers.Register<DiscoveryRejected>(this.On);
        handlers.Register<DiscoveryFailed>(this.On);
        return handlers;
    }

    private void On(DiscoveryRequested @event)
    {
        criteria = @event.Criteria;
        status = CatalogSearchLifecycleStatus.Requested;
        priority = null;
        willBeLookedUp = true;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = null;
        updatedAt = @event.RequestedAt;
        hasRequested = true;
    }

    private void On(DiscoveryPlanned @event)
    {
        criteria = @event.Criteria;
        status = CatalogSearchLifecycleStatus.Planned;
        priority = @event.Priority;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = @event.EstimatedRetryAfterSeconds;
        earliestExpectedCompletionAt = @event.EarliestExpectedCompletionAt;
        reason = @event.Reason;
        updatedAt = @event.PlannedAt;
        hasRequested = true;
    }

    private void On(DiscoveryDeferred @event)
    {
        criteria = @event.Criteria;
        status = CatalogSearchLifecycleStatus.Deferred;
        priority = null;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = @event.EstimatedRetryAfterSeconds;
        earliestExpectedCompletionAt = @event.EarliestExpectedCompletionAt;
        reason = @event.Reason;
        updatedAt = @event.DeferredAt;
        hasRequested = true;
    }

    private void On(DiscoveryRejected @event)
    {
        criteria = @event.Criteria;
        status = CatalogSearchLifecycleStatus.Rejected;
        priority = null;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = @event.Reason;
        updatedAt = @event.RejectedAt;
        hasRequested = true;
    }

    private void On(DiscoveryFailed @event)
    {
        criteria = @event.Criteria;
        status = CatalogSearchLifecycleStatus.Failed;
        priority = null;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = @event.Reason;
        updatedAt = @event.FailedAt;
        hasRequested = true;
    }
}
