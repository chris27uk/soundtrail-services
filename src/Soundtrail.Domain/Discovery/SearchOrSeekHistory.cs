using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed class SearchOrSeekHistory
{
    private readonly EventHandlers<SearchOrSeekHistory> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private MusicSeekOrSearchCriteria? criteria;
    private CatalogSearchLifecycleStatus? status;
    private LookupPriorityBand? priority;
    private bool willBeLookedUp;
    private int? estimatedRetryAfterSeconds;
    private DateTimeOffset? earliestExpectedCompletionAt;
    private string? reason;
    private DateTimeOffset? updatedAt;
    private bool hasRequested;
    private int version;

    private SearchOrSeekHistory(IEnumerable<IDomainEvent> events, int version)
    {
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }

        this.version = version;
    }

    public static async Task<SearchOrSeekHistory> LoadAsync(
        ICatalogSearchDiscoveryRepository repository,
        MusicSeekOrSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(criteria, cancellationToken);
        var aggregate = new SearchOrSeekHistory(
            stream.Events.Where(static @event =>
                @event is MusicMetadataRequired
                or Events.StreamingLocationsRequired
                or DiscoveryRequested
                or DiscoveryPlanned
                or DiscoveryDeferred
                or DiscoveryRejected
                or DiscoveryFailed
                or DiscoveryStarted
                or DiscoveryCompleted),
            stream.Version);
        aggregate.criteria ??= criteria;
        return aggregate;
    }

    public bool Request(CatalogSearchRequested requested) =>
        Request(
            requested.Criteria.RequireSearchCriteria(),
            requested.TrustLevel,
            requested.RiskScore,
            requested.OccurredAt,
            requested.CorrelationId);

    public bool Request(
        MusicSearchCriteria searchCriteria,
        int trustLevel,
        int riskScore,
        DateTimeOffset requestedAt,
        CorrelationId correlationId)
    {
        if (hasRequested)
        {
            return false;
        }

        Apply(
            new DiscoveryRequested(
                searchCriteria,
                trustLevel,
                riskScore,
                requestedAt,
                correlationId),
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
                RequireSearchCriteria(),
                priority,
                true,
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
                RequireSearchCriteria(),
                true,
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
                RequireSearchCriteria(),
                false,
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
                RequireSearchCriteria(),
                false,
                reason,
                failedAt),
            isNew: true);

        return true;
    }

    public bool Start(LookupPriorityBand priority, string reason, DateTimeOffset startedAt)
    {
        EnsureCanTransitionTo(CatalogSearchLifecycleStatus.InProgress);

        if (status == CatalogSearchLifecycleStatus.InProgress
            && this.priority == priority
            && willBeLookedUp
            && string.Equals(this.reason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new DiscoveryStarted(
                RequireSearchCriteria(),
                priority,
                true,
                reason,
                startedAt),
            isNew: true);

        return true;
    }

    public bool Complete(
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset completedAt)
    {
        EnsureCanTransitionTo(CatalogSearchLifecycleStatus.Completed);

        if (status == CatalogSearchLifecycleStatus.Completed
            && this.priority == priority
            && !willBeLookedUp
            && string.Equals(this.reason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new DiscoveryCompleted(
                RequireSearchCriteria(),
                priority,
                false,
                reason,
                completedAt),
            isNew: true);

        return true;
    }

    public void StreamingLocationsRequired(
        MusicCatalogId musicCatalogId,
        LookupPriorityBand priority,
        DateTimeOffset observedAt,
        CorrelationId correlationId,
        MusicSearchCriteria lookupSearchCriteria,
        CatalogTrackHierarchy? hierarchy = null)
    {
        Apply(
            new StreamingLocationsRequired(
                musicCatalogId,
                priority,
                correlationId,
                ProviderName.MusicBrainz,
                observedAt,
                lookupSearchCriteria,
                hierarchy),
            isNew: true);
    }

    public void MetadataRequired(
        int trustLevel,
        int riskScore,
        DateTimeOffset requiredAt,
        CorrelationId correlationId)
    {
        Apply(
            new MusicMetadataRequired(
                this.criteria ?? throw new InvalidOperationException("Catalog search term has not been established."),
                trustLevel,
                riskScore,
                requiredAt,
                correlationId),
            isNew: true);
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
            (this.criteria ?? throw new InvalidOperationException("Catalog search term has not been established.")),
            this.version,
            this.uncommittedEvents.AsReadOnly(),
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

    private EventHandlers<SearchOrSeekHistory> CreateHandlers()
    {
        var handlers = new EventHandlers<SearchOrSeekHistory>();
        handlers.Register<MusicMetadataRequired>(On);
        handlers.Register<StreamingLocationsRequired>(On);
        handlers.Register<DiscoveryRequested>(On);
        handlers.Register<DiscoveryPlanned>(On);
        handlers.Register<DiscoveryDeferred>(On);
        handlers.Register<DiscoveryRejected>(On);
        handlers.Register<DiscoveryFailed>(On);
        handlers.Register<DiscoveryStarted>(On);
        handlers.Register<DiscoveryCompleted>(On);
        return handlers;
    }

    private void On(MusicMetadataRequired @event)
    {
        this.criteria = @event.Criteria;
    }

    private void On(StreamingLocationsRequired @event)
    {
        _ = @event;
    }

    private void On(DiscoveryRequested @event)
    {
        criteria = MusicSeekOrSearchCriteria.FromSearch(@event.SearchCriteria);
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
        criteria = MusicSeekOrSearchCriteria.FromSearch(@event.SearchCriteria);
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
        criteria = MusicSeekOrSearchCriteria.FromSearch(@event.SearchCriteria);
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
        criteria = MusicSeekOrSearchCriteria.FromSearch(@event.SearchCriteria);
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
        criteria = MusicSeekOrSearchCriteria.FromSearch(@event.SearchCriteria);
        status = CatalogSearchLifecycleStatus.Failed;
        priority = null;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = @event.Reason;
        updatedAt = @event.FailedAt;
        hasRequested = true;
    }

    private void On(DiscoveryStarted @event)
    {
        criteria = MusicSeekOrSearchCriteria.FromSearch(@event.SearchCriteria);
        status = CatalogSearchLifecycleStatus.InProgress;
        priority = @event.Priority;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = @event.Reason;
        updatedAt = @event.StartedAt;
        hasRequested = true;
    }

    private void On(DiscoveryCompleted @event)
    {
        criteria = MusicSeekOrSearchCriteria.FromSearch(@event.SearchCriteria);
        status = CatalogSearchLifecycleStatus.Completed;
        priority = @event.Priority;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = @event.Reason;
        updatedAt = @event.CompletedAt;
        hasRequested = true;
    }

    private MusicSearchCriteria RequireSearchCriteria() =>
        (criteria ?? throw new InvalidOperationException("Catalog search term has not been established."))
        .RequireSearchCriteria();

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
}
