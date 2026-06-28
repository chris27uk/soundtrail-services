using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed class KnownItemDiscovery
{
    private readonly EventHandlers<KnownItemDiscovery> eventHandlers;
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private KnownCatalogItem? knownItem;
    private bool hasKnownTrackRequested;
    private bool hasArtistCatalogLookupRequested;
    private bool hasAlbumCatalogLookupRequested;
    private CatalogSearchLifecycleStatus? knownTrackStatus;
    private LookupPriorityBand? knownTrackPriority;
    private int? knownTrackEstimatedRetryAfterSeconds;
    private DateTimeOffset? knownTrackEarliestExpectedCompletionAt;
    private string? knownTrackReason;

    private KnownItemDiscovery(IEnumerable<IDomainEvent> events)
    {
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<DiscoveryQueryKey, IDomainEvent> Stream, KnownItemDiscovery Aggregate)> LoadAsync(
        IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> repository,
        KnownCatalogItem knownItem,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(DiscoveryQueryKey.For(knownItem), cancellationToken);
        var aggregate = new KnownItemDiscovery(stream.Events);
        aggregate.knownItem ??= knownItem;
        return (stream, aggregate);
    }

    public bool TrackRequested(
        TrackId trackId,
        PlaybackProviderFilter playback,
        DateTimeOffset requestedAt,
        CorrelationId correlationId)
    {
        if (hasKnownTrackRequested)
        {
            return false;
        }

        Apply(
            new Events.KnownTrackRequested(
                trackId,
                playback,
                requestedAt,
                correlationId),
            isNew: true);

        return true;
    }

    public bool TrackLookupStarted(
        TrackId trackId,
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset startedAt)
    {
        if (!hasKnownTrackRequested)
        {
            return false;
        }

        if (knownTrackStatus == CatalogSearchLifecycleStatus.InProgress
            && knownTrackPriority == priority
            && string.Equals(knownTrackReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new KnownTrackDiscoveryStarted(
                trackId,
                priority,
                reason,
                startedAt),
            isNew: true);

        return true;
    }

    public bool TrackLookupCompleted(
        TrackId trackId,
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset completedAt)
    {
        if (!hasKnownTrackRequested)
        {
            return false;
        }

        if (knownTrackStatus == CatalogSearchLifecycleStatus.Completed
            && knownTrackPriority == priority
            && string.Equals(knownTrackReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new KnownTrackDiscoveryCompleted(
                trackId,
                priority,
                reason,
                completedAt),
            isNew: true);

        return true;
    }

    public bool TrackLookupDeferred(
        TrackId trackId,
        int? estimatedRetryAfterSeconds,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        DateTimeOffset deferredAt)
    {
        if (!hasKnownTrackRequested)
        {
            return false;
        }

        if (knownTrackStatus == CatalogSearchLifecycleStatus.Deferred
            && knownTrackEstimatedRetryAfterSeconds == estimatedRetryAfterSeconds
            && knownTrackEarliestExpectedCompletionAt == earliestExpectedCompletionAt
            && string.Equals(knownTrackReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new KnownTrackDiscoveryDeferred(
                trackId,
                estimatedRetryAfterSeconds,
                earliestExpectedCompletionAt,
                reason,
                deferredAt),
            isNew: true);

        return true;
    }

    public bool TrackLookupFailed(
        TrackId trackId,
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset failedAt)
    {
        if (!hasKnownTrackRequested)
        {
            return false;
        }

        if (knownTrackStatus == CatalogSearchLifecycleStatus.Failed
            && knownTrackPriority == priority
            && string.Equals(knownTrackReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new KnownTrackDiscoveryFailed(
                trackId,
                priority,
                reason,
                failedAt),
            isNew: true);

        return true;
    }

    public bool ArtistRequested(
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

    public bool AlbumRequested(
        ArtistId? artistId,
        AlbumId albumId,
        DateTimeOffset requestedAt,
        CorrelationId correlationId)
    {
        if (hasAlbumCatalogLookupRequested)
        {
            return false;
        }

        Apply(
            new AlbumCatalogLookupRequested(
                artistId,
                albumId,
                requestedAt,
                correlationId),
            isNew: true);

        return true;
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

        _ = knownItem ?? throw new InvalidOperationException("Known catalog item has not been established.");

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

    private EventHandlers<KnownItemDiscovery> CreateHandlers()
    {
        var handlers = new EventHandlers<KnownItemDiscovery>();
        handlers.Register<Events.KnownTrackRequested>(On);
        handlers.Register<KnownTrackDiscoveryStarted>(On);
        handlers.Register<KnownTrackDiscoveryCompleted>(On);
        handlers.Register<KnownTrackDiscoveryDeferred>(On);
        handlers.Register<KnownTrackDiscoveryFailed>(On);
        handlers.Register<ArtistCatalogLookupRequested>(On);
        handlers.Register<AlbumCatalogLookupRequested>(On);
        return handlers;
    }

    private void On(Events.KnownTrackRequested @event)
    {
        knownItem ??= KnownCatalogItem.ForTrack(@event.TrackId);
        hasKnownTrackRequested = true;
    }

    private void On(KnownTrackDiscoveryStarted @event)
    {
        knownItem ??= KnownCatalogItem.ForTrack(@event.TrackId);
        knownTrackStatus = CatalogSearchLifecycleStatus.InProgress;
        knownTrackPriority = @event.Priority;
        knownTrackEstimatedRetryAfterSeconds = null;
        knownTrackEarliestExpectedCompletionAt = null;
        knownTrackReason = @event.Reason;
    }

    private void On(KnownTrackDiscoveryCompleted @event)
    {
        knownItem ??= KnownCatalogItem.ForTrack(@event.TrackId);
        knownTrackStatus = CatalogSearchLifecycleStatus.Completed;
        knownTrackPriority = @event.Priority;
        knownTrackEstimatedRetryAfterSeconds = null;
        knownTrackEarliestExpectedCompletionAt = null;
        knownTrackReason = @event.Reason;
    }

    private void On(KnownTrackDiscoveryDeferred @event)
    {
        knownItem ??= KnownCatalogItem.ForTrack(@event.TrackId);
        knownTrackStatus = CatalogSearchLifecycleStatus.Deferred;
        knownTrackPriority = null;
        knownTrackEstimatedRetryAfterSeconds = @event.EstimatedRetryAfterSeconds;
        knownTrackEarliestExpectedCompletionAt = @event.EarliestExpectedCompletionAt;
        knownTrackReason = @event.Reason;
    }

    private void On(KnownTrackDiscoveryFailed @event)
    {
        knownItem ??= KnownCatalogItem.ForTrack(@event.TrackId);
        knownTrackStatus = CatalogSearchLifecycleStatus.Failed;
        knownTrackPriority = @event.Priority;
        knownTrackEstimatedRetryAfterSeconds = null;
        knownTrackEarliestExpectedCompletionAt = null;
        knownTrackReason = @event.Reason;
    }

    private void On(ArtistCatalogLookupRequested @event)
    {
        knownItem ??= KnownCatalogItem.ForArtist(@event.ArtistId);
        hasArtistCatalogLookupRequested = true;
    }

    private void On(AlbumCatalogLookupRequested @event)
    {
        knownItem ??= KnownCatalogItem.ForAlbum(
            @event.ArtistId ?? throw new InvalidOperationException("Album lookup requests must include an artist id."),
            @event.AlbumId);
        hasAlbumCatalogLookupRequested = true;
    }
}
