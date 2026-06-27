using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed class SearchOrSeekHistory
{
    private readonly EventHandlers<SearchOrSeekHistory> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private MusicSearchCriteria? criteria;
    private KnownCatalogItem? knownItem;
    private CatalogSearchLifecycleStatus? status;
    private LookupPriorityBand? priority;
    private bool willBeLookedUp;
    private int? estimatedRetryAfterSeconds;
    private DateTimeOffset? earliestExpectedCompletionAt;
    private string? reason;
    private bool hasRequested;
    private bool hasKnownTrackRequested;
    private bool hasTrackMetadataLookupRequested;
    private bool hasArtistCatalogLookupRequested;
    private bool hasAlbumCatalogLookupRequested;
    private bool hasStreamingLocationsRequired;
    private int version;

    private SearchOrSeekHistory(IEnumerable<IDomainEvent> events, int version)
    {
        this.eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }

        this.version = version;
    }

    public static async Task<SearchOrSeekHistory> LoadAsync(ICatalogSearchDiscoveryRepository repository, MusicSearchCriteria searchCriteria, CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(searchCriteria, cancellationToken);
        var aggregate = new SearchOrSeekHistory(stream.Events, stream.Version);
        aggregate.criteria ??= searchCriteria;
        return aggregate;
    }

    public static async Task<SearchOrSeekHistory> LoadAsync(ICatalogSearchDiscoveryRepository repository, KnownCatalogItem knownItem, CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(knownItem, cancellationToken);
        var aggregate = new SearchOrSeekHistory(stream.Events, stream.Version);
        aggregate.knownItem ??= knownItem;
        return aggregate;
    }

    public bool SearchRequested(SearchCatalogRequested requested) =>
        Request(
            requested.SearchCriteria,
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
                LookupSource.MusicBrainz,
                observedAt,
                lookupSearchCriteria,
                hierarchy),
            isNew: true);
    }

    public void KnownTrackRequested(
        TrackId trackId,
        PlaybackProviderFilter playback,
        DateTimeOffset requestedAt,
        CorrelationId correlationId)
    {
        if (hasKnownTrackRequested)
        {
            return;
        }

        Apply(
            new Events.KnownTrackRequested(
                trackId,
                playback,
                requestedAt,
                correlationId),
            isNew: true);
    }

    public bool RequireStreamingLocationsForKnownTrack(
        MusicCatalogId musicCatalogId,
        LookupPriorityBand priority,
        DateTimeOffset observedAt,
        CorrelationId correlationId,
        MusicSearchCriteria lookupSearchCriteria,
        CatalogTrackHierarchy? hierarchy = null)
    {
        if (hasStreamingLocationsRequired)
        {
            return false;
        }

        Apply(
            new StreamingLocationsRequired(
                musicCatalogId,
                priority,
                correlationId,
                LookupSource.MusicBrainz,
                observedAt,
                lookupSearchCriteria,
                hierarchy),
            isNew: true);

        return true;
    }

    public void MetadataRequired(
        int trustLevel,
        int riskScore,
        DateTimeOffset requiredAt,
        CorrelationId correlationId)
    {
        Apply(
            new TrackMetadataLookupRequested(
                this.criteria ?? throw new InvalidOperationException("Catalog search term has not been established."),
                trustLevel,
                riskScore,
                requiredAt,
                correlationId),
            isNew: true);
    }

    public bool TrackMetadataLookupRequested(
        MusicSearchCriteria searchCriteria,
        int trustLevel,
        int riskScore,
        DateTimeOffset requiredAt,
        CorrelationId correlationId)
    {
        if (hasTrackMetadataLookupRequested)
        {
            return false;
        }

        Apply(
            new TrackMetadataLookupRequested(
                searchCriteria,
                trustLevel,
                riskScore,
                requiredAt,
                correlationId),
            isNew: true);

        return true;
    }

    public bool ArtistCatalogLookupRequested(
        ArtistId artistId,
        DateTimeOffset requestedAt,
        CorrelationId correlationId)
    {
        if (hasArtistCatalogLookupRequested)
        {
            return false;
        }

        Apply(
            new ArtistCatalogLookupRequested(
                artistId,
                requestedAt,
                correlationId),
            isNew: true);

        return true;
    }

    public void AlbumCatalogLookupRequested(
        ArtistId? artistId,
        AlbumId albumId,
        DateTimeOffset requestedAt,
        CorrelationId correlationId)
    {
        if (hasAlbumCatalogLookupRequested)
        {
            return;
        }

        Apply(
            new AlbumCatalogLookupRequested(
                artistId,
                albumId,
                requestedAt,
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

        var saved = criteria is not null
            ? await repository.AppendAsync(
                criteria,
                version,
                uncommittedEvents.AsReadOnly(),
                cancellationToken)
            : knownItem is not null
                ? await repository.AppendAsync(
                    knownItem,
                    version,
                    uncommittedEvents.AsReadOnly(),
                    cancellationToken)
                : throw new InvalidOperationException("Catalog discovery subject has not been established.");

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
        handlers.Register<TrackMetadataLookupRequested>(On);
        handlers.Register<Events.KnownTrackRequested>(On);
        handlers.Register<ArtistCatalogLookupRequested>(On);
        handlers.Register<AlbumCatalogLookupRequested>(On);
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

    private void On(TrackMetadataLookupRequested @event)
    {
        this.criteria = @event.SearchCriteria;
        hasTrackMetadataLookupRequested = true;
    }

    private void On(Events.KnownTrackRequested @event)
    {
        knownItem ??= KnownCatalogItem.ForTrack(@event.TrackId);
        hasKnownTrackRequested = true;
    }

    private void On(ArtistCatalogLookupRequested @event)
    {
        knownItem ??= KnownCatalogItem.ForArtist(@event.ArtistId);
        hasArtistCatalogLookupRequested = true;
    }

    private void On(AlbumCatalogLookupRequested @event)
    {
        knownItem ??= KnownCatalogItem.ForAlbum(@event.AlbumId);
        hasAlbumCatalogLookupRequested = true;
    }

    private void On(StreamingLocationsRequired @event)
    {
        _ = @event;
        hasStreamingLocationsRequired = true;
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

    private MusicSearchCriteria RequireSearchCriteria() =>
        criteria ?? throw new InvalidOperationException("Catalog search term has not been established.");

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
