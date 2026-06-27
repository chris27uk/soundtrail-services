using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Discovery;

public sealed class DiscoveryLifecycleProjection
{
    private readonly EventHandlers<DiscoveryLifecycleProjection> eventHandlers;

    public DiscoveryLifecycleProjection(MusicSearchCriteria searchCriteria)
    {
        SearchCriteria = searchCriteria;
        eventHandlers = CreateHandlers();
    }

    public MusicSearchCriteria SearchCriteria { get; }

    public string Status { get; private set; } = string.Empty;

    public string Priority { get; private set; } = string.Empty;

    public bool WillBeLookedUp { get; private set; }

    public int? EstimatedRetryAfterSeconds { get; private set; }

    public DateTimeOffset? EarliestExpectedCompletionAt { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public int ProjectionVersion { get; private set; }

    public static DiscoveryLifecycleProjection Load(DiscoveryLifecycleProjectionSnapshot snapshot) =>
        new(snapshot.SearchCriteria)
        {
            Status = snapshot.Status,
            Priority = snapshot.Priority,
            WillBeLookedUp = snapshot.WillBeLookedUp,
            EstimatedRetryAfterSeconds = snapshot.EstimatedRetryAfterSeconds,
            EarliestExpectedCompletionAt = snapshot.EarliestExpectedCompletionAt,
            Reason = snapshot.Reason,
            UpdatedAt = snapshot.UpdatedAt,
            ProjectionVersion = snapshot.ProjectionVersion
        };

    public DiscoveryLifecycleProjectionSnapshot ToSnapshot() =>
        new(
            SearchCriteria,
            Status,
            Priority,
            WillBeLookedUp,
            EstimatedRetryAfterSeconds,
            EarliestExpectedCompletionAt,
            Reason,
            UpdatedAt,
            ProjectionVersion);

    public void Apply(IDomainEvent @event, int version)
    {
        eventHandlers.Handle(@event);
        ProjectionVersion = version;
    }

    private EventHandlers<DiscoveryLifecycleProjection> CreateHandlers()
    {
        var handlers = new EventHandlers<DiscoveryLifecycleProjection>();

        handlers.Register<MusicMetadataRequired>(_ => { });
        handlers.Register<StreamingLocationsRequired>(_ => { });

        handlers.Register<DiscoveryRequested>(@event =>
        {
            Status = CatalogSearchLifecycleStatus.Requested.ToString();
            Priority = string.Empty;
            WillBeLookedUp = true;
            EstimatedRetryAfterSeconds = null;
            EarliestExpectedCompletionAt = null;
            Reason = "Discovery requested";
            UpdatedAt = @event.RequestedAt;
        });

        handlers.Register<DiscoveryPlanned>(@event =>
        {
            Status = CatalogSearchLifecycleStatus.Planned.ToString();
            Priority = @event.Priority.ToString();
            WillBeLookedUp = @event.WillBeLookedUp;
            EstimatedRetryAfterSeconds = @event.EstimatedRetryAfterSeconds;
            EarliestExpectedCompletionAt = @event.EarliestExpectedCompletionAt;
            Reason = @event.Reason;
            UpdatedAt = @event.PlannedAt;
        });

        handlers.Register<DiscoveryDeferred>(@event =>
        {
            Status = CatalogSearchLifecycleStatus.Deferred.ToString();
            Priority = string.Empty;
            WillBeLookedUp = @event.WillBeLookedUp;
            EstimatedRetryAfterSeconds = @event.EstimatedRetryAfterSeconds;
            EarliestExpectedCompletionAt = @event.EarliestExpectedCompletionAt;
            Reason = @event.Reason;
            UpdatedAt = @event.DeferredAt;
        });

        handlers.Register<DiscoveryRejected>(@event =>
        {
            Status = CatalogSearchLifecycleStatus.Rejected.ToString();
            Priority = string.Empty;
            WillBeLookedUp = @event.WillBeLookedUp;
            EstimatedRetryAfterSeconds = null;
            EarliestExpectedCompletionAt = null;
            Reason = @event.Reason;
            UpdatedAt = @event.RejectedAt;
        });

        handlers.Register<DiscoveryFailed>(@event =>
        {
            Status = CatalogSearchLifecycleStatus.Failed.ToString();
            Priority = string.Empty;
            WillBeLookedUp = @event.WillBeLookedUp;
            EstimatedRetryAfterSeconds = null;
            EarliestExpectedCompletionAt = null;
            Reason = @event.Reason;
            UpdatedAt = @event.FailedAt;
        });

        handlers.Register<DiscoveryStarted>(@event =>
        {
            Status = CatalogSearchLifecycleStatus.InProgress.ToString();
            Priority = @event.Priority.ToString();
            WillBeLookedUp = @event.WillBeLookedUp;
            EstimatedRetryAfterSeconds = null;
            EarliestExpectedCompletionAt = null;
            Reason = @event.Reason;
            UpdatedAt = @event.StartedAt;
        });

        handlers.Register<DiscoveryCompleted>(@event =>
        {
            Status = CatalogSearchLifecycleStatus.Completed.ToString();
            Priority = @event.Priority.ToString();
            WillBeLookedUp = @event.WillBeLookedUp;
            EstimatedRetryAfterSeconds = null;
            EarliestExpectedCompletionAt = null;
            Reason = @event.Reason;
            UpdatedAt = @event.CompletedAt;
        });

        return handlers;
    }
}
