using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed class SearchDiscoveryHistory
{
    private readonly EventHandlers<SearchDiscoveryHistory> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private readonly HashSet<string> recordedCandidateKeys = new(StringComparer.Ordinal);
    private MusicSearchCriteria? criteria;
    private CatalogSearchLifecycleStatus? status;
    private LookupPriorityBand? priority;
    private bool willBeLookedUp;
    private int? estimatedRetryAfterSeconds;
    private DateTimeOffset? earliestExpectedCompletionAt;
    private string? reason;
    private bool hasRequested;

    private SearchDiscoveryHistory(IEnumerable<IDomainEvent> events)
    {
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<DiscoveryQueryKey, IDomainEvent> Stream, SearchDiscoveryHistory Aggregate)> LoadAsync(
        IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> repository,
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(DiscoveryQueryKey.For(searchCriteria), cancellationToken);
        var aggregate = new SearchDiscoveryHistory(stream.Events);
        aggregate.criteria ??= searchCriteria;
        return (stream, aggregate);
    }

    public bool SearchRequested(SearchCatalogRequested requested) =>
        Request(
            requested.SearchCriteria,
            requested.Playback,
            requested.TrustLevel,
            requested.RiskScore,
            requested.OccurredAt,
            requested.CorrelationId);

    public bool Request(
        MusicSearchCriteria searchCriteria,
        PlaybackProviderFilter? playback,
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
                playback,
                trustLevel,
                riskScore,
                requestedAt,
                correlationId),
            isNew: true);

        return true;
    }

    public bool IdentifyCatalogCandidate(
        MusicCatalogId musicCatalogId,
        int trustLevel,
        int riskScore,
        DateTimeOffset recordedAt,
        CorrelationId correlationId)
    {
        var candidateKey = ToCandidateKey(musicCatalogId, correlationId);
        if (recordedCandidateKeys.Contains(candidateKey))
        {
            return false;
        }

        Apply(
            new CatalogCandidateIdentified(
                RequireSearchCriteria(),
                musicCatalogId,
                trustLevel,
                riskScore,
                recordedAt,
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

    public bool LookupStarted(
        LookupPriorityBand priority,
        DateTimeOffset startedAt) =>
        Start(priority, "Lookup started", startedAt);

    public bool LookupCompleted(
        LookupPriorityBand priority,
        DateTimeOffset completedAt) =>
        Complete(priority, "Discovery completed", completedAt);

    public bool LookupDeferred(
        int? estimatedRetryAfterSeconds,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        DateTimeOffset deferredAt) =>
        Defer(
            estimatedRetryAfterSeconds,
            earliestExpectedCompletionAt,
            reason,
            deferredAt);

    public bool LookupFailed(
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset failedAt)
    {
        LookupStarted(priority, failedAt);
        return Fail(reason, failedAt);
    }

    public async Task<bool> SaveAsync(
        IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> repository,
        LoadedEventStream<DiscoveryQueryKey, IDomainEvent> stream,
        CancellationToken cancellationToken)
    {
        if (uncommittedEvents.Count == 0)
        {
            return true;
        }

        _ = criteria ?? throw new InvalidOperationException("Catalog search term has not been established.");

        var saved = (await repository.AppendAsync(
            stream,
            uncommittedEvents.AsReadOnly(),
            null,
            cancellationToken)).Appended;

        if (saved)
        {
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

    private EventHandlers<SearchDiscoveryHistory> CreateHandlers()
    {
        var handlers = new EventHandlers<SearchDiscoveryHistory>();
        handlers.Register<DiscoveryRequested>(On);
        handlers.Register<DiscoveryPlanned>(On);
        handlers.Register<DiscoveryDeferred>(On);
        handlers.Register<DiscoveryRejected>(On);
        handlers.Register<DiscoveryFailed>(On);
        handlers.Register<DiscoveryStarted>(On);
        handlers.Register<DiscoveryCompleted>(On);
        handlers.Register<CatalogCandidateIdentified>(On);
        return handlers;
    }

    private void On(DiscoveryRequested @event)
    {
        criteria = @event.SearchCriteria;
        status = CatalogSearchLifecycleStatus.Requested;
        priority = null;
        willBeLookedUp = true;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = null;
        hasRequested = true;
    }

    private void On(DiscoveryPlanned @event)
    {
        criteria = @event.SearchCriteria;
        status = CatalogSearchLifecycleStatus.Planned;
        priority = @event.Priority;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = @event.EstimatedRetryAfterSeconds;
        earliestExpectedCompletionAt = @event.EarliestExpectedCompletionAt;
        reason = @event.Reason;
        hasRequested = true;
    }

    private void On(DiscoveryDeferred @event)
    {
        criteria = @event.SearchCriteria;
        status = CatalogSearchLifecycleStatus.Deferred;
        priority = null;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = @event.EstimatedRetryAfterSeconds;
        earliestExpectedCompletionAt = @event.EarliestExpectedCompletionAt;
        reason = @event.Reason;
        hasRequested = true;
    }

    private void On(DiscoveryRejected @event)
    {
        criteria = @event.SearchCriteria;
        status = CatalogSearchLifecycleStatus.Rejected;
        priority = null;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = @event.Reason;
        hasRequested = true;
    }

    private void On(DiscoveryFailed @event)
    {
        criteria = @event.SearchCriteria;
        status = CatalogSearchLifecycleStatus.Failed;
        priority = null;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = @event.Reason;
        hasRequested = true;
    }

    private void On(DiscoveryStarted @event)
    {
        criteria = @event.SearchCriteria;
        status = CatalogSearchLifecycleStatus.InProgress;
        priority = @event.Priority;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = @event.Reason;
        hasRequested = true;
    }

    private void On(DiscoveryCompleted @event)
    {
        criteria = @event.SearchCriteria;
        status = CatalogSearchLifecycleStatus.Completed;
        priority = @event.Priority;
        willBeLookedUp = @event.WillBeLookedUp;
        estimatedRetryAfterSeconds = null;
        earliestExpectedCompletionAt = null;
        reason = @event.Reason;
        hasRequested = true;
    }

    private void On(CatalogCandidateIdentified @event)
    {
        criteria = @event.SearchCriteria;
        recordedCandidateKeys.Add(ToCandidateKey(@event.MusicCatalogId, @event.CorrelationId));
    }

    private MusicSearchCriteria RequireSearchCriteria() =>
        criteria ?? throw new InvalidOperationException("Catalog search term has not been established.");

    private static string ToCandidateKey(MusicCatalogId musicCatalogId, CorrelationId correlationId) =>
        $"{correlationId.Value}:{musicCatalogId.Value}";

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
