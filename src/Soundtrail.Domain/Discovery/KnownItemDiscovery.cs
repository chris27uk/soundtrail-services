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
    private CatalogSearchLifecycleStatus? artistStatus;
    private CatalogSearchLifecycleStatus? albumStatus;
    private LookupPriorityBand? knownTrackPriority;
    private LookupPriorityBand? artistPriority;
    private LookupPriorityBand? albumPriority;
    private int? knownTrackEstimatedRetryAfterSeconds;
    private int? artistEstimatedRetryAfterSeconds;
    private int? albumEstimatedRetryAfterSeconds;
    private DateTimeOffset? knownTrackEarliestExpectedCompletionAt;
    private DateTimeOffset? artistEarliestExpectedCompletionAt;
    private DateTimeOffset? albumEarliestExpectedCompletionAt;
    private string? knownTrackReason;
    private string? artistReason;
    private string? albumReason;

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

    public bool ArtistLookupStarted(
        ArtistId artistId,
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset startedAt)
    {
        if (!hasArtistCatalogLookupRequested)
        {
            return false;
        }

        if (artistStatus == CatalogSearchLifecycleStatus.InProgress
            && artistPriority == priority
            && string.Equals(artistReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(new KnownArtistDiscoveryStarted(artistId, priority, reason, startedAt), isNew: true);
        return true;
    }

    public bool ArtistLookupCompleted(
        ArtistId artistId,
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset completedAt)
    {
        if (!hasArtistCatalogLookupRequested)
        {
            return false;
        }

        if (artistStatus == CatalogSearchLifecycleStatus.Completed
            && artistPriority == priority
            && string.Equals(artistReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(new KnownArtistDiscoveryCompleted(artistId, priority, reason, completedAt), isNew: true);
        return true;
    }

    public bool ArtistLookupDeferred(
        ArtistId artistId,
        int? estimatedRetryAfterSeconds,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        DateTimeOffset deferredAt)
    {
        if (!hasArtistCatalogLookupRequested)
        {
            return false;
        }

        if (artistStatus == CatalogSearchLifecycleStatus.Deferred
            && artistEstimatedRetryAfterSeconds == estimatedRetryAfterSeconds
            && artistEarliestExpectedCompletionAt == earliestExpectedCompletionAt
            && string.Equals(artistReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new KnownArtistDiscoveryDeferred(
                artistId,
                estimatedRetryAfterSeconds,
                earliestExpectedCompletionAt,
                reason,
                deferredAt),
            isNew: true);
        return true;
    }

    public bool ArtistLookupFailed(
        ArtistId artistId,
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset failedAt)
    {
        if (!hasArtistCatalogLookupRequested)
        {
            return false;
        }

        if (artistStatus == CatalogSearchLifecycleStatus.Failed
            && artistPriority == priority
            && string.Equals(artistReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(new KnownArtistDiscoveryFailed(artistId, priority, reason, failedAt), isNew: true);
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

    public bool AlbumLookupStarted(
        ArtistId artistId,
        AlbumId albumId,
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset startedAt)
    {
        if (!hasAlbumCatalogLookupRequested)
        {
            return false;
        }

        if (albumStatus == CatalogSearchLifecycleStatus.InProgress
            && albumPriority == priority
            && string.Equals(albumReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(new KnownAlbumDiscoveryStarted(artistId, albumId, priority, reason, startedAt), isNew: true);
        return true;
    }

    public bool AlbumLookupCompleted(
        ArtistId artistId,
        AlbumId albumId,
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset completedAt)
    {
        if (!hasAlbumCatalogLookupRequested)
        {
            return false;
        }

        if (albumStatus == CatalogSearchLifecycleStatus.Completed
            && albumPriority == priority
            && string.Equals(albumReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(new KnownAlbumDiscoveryCompleted(artistId, albumId, priority, reason, completedAt), isNew: true);
        return true;
    }

    public bool AlbumLookupDeferred(
        ArtistId artistId,
        AlbumId albumId,
        int? estimatedRetryAfterSeconds,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        DateTimeOffset deferredAt)
    {
        if (!hasAlbumCatalogLookupRequested)
        {
            return false;
        }

        if (albumStatus == CatalogSearchLifecycleStatus.Deferred
            && albumEstimatedRetryAfterSeconds == estimatedRetryAfterSeconds
            && albumEarliestExpectedCompletionAt == earliestExpectedCompletionAt
            && string.Equals(albumReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(
            new KnownAlbumDiscoveryDeferred(
                artistId,
                albumId,
                estimatedRetryAfterSeconds,
                earliestExpectedCompletionAt,
                reason,
                deferredAt),
            isNew: true);
        return true;
    }

    public bool AlbumLookupFailed(
        ArtistId artistId,
        AlbumId albumId,
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset failedAt)
    {
        if (!hasAlbumCatalogLookupRequested)
        {
            return false;
        }

        if (albumStatus == CatalogSearchLifecycleStatus.Failed
            && albumPriority == priority
            && string.Equals(albumReason, reason, StringComparison.Ordinal))
        {
            return false;
        }

        Apply(new KnownAlbumDiscoveryFailed(artistId, albumId, priority, reason, failedAt), isNew: true);
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
        handlers.Register<KnownArtistDiscoveryStarted>(On);
        handlers.Register<KnownArtistDiscoveryCompleted>(On);
        handlers.Register<KnownArtistDiscoveryDeferred>(On);
        handlers.Register<KnownArtistDiscoveryFailed>(On);
        handlers.Register<KnownAlbumDiscoveryStarted>(On);
        handlers.Register<KnownAlbumDiscoveryCompleted>(On);
        handlers.Register<KnownAlbumDiscoveryDeferred>(On);
        handlers.Register<KnownAlbumDiscoveryFailed>(On);
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

    private void On(KnownArtistDiscoveryStarted @event)
    {
        knownItem ??= KnownCatalogItem.ForArtist(@event.ArtistId);
        artistStatus = CatalogSearchLifecycleStatus.InProgress;
        artistPriority = @event.Priority;
        artistEstimatedRetryAfterSeconds = null;
        artistEarliestExpectedCompletionAt = null;
        artistReason = @event.Reason;
    }

    private void On(KnownArtistDiscoveryCompleted @event)
    {
        knownItem ??= KnownCatalogItem.ForArtist(@event.ArtistId);
        artistStatus = CatalogSearchLifecycleStatus.Completed;
        artistPriority = @event.Priority;
        artistEstimatedRetryAfterSeconds = null;
        artistEarliestExpectedCompletionAt = null;
        artistReason = @event.Reason;
    }

    private void On(KnownArtistDiscoveryDeferred @event)
    {
        knownItem ??= KnownCatalogItem.ForArtist(@event.ArtistId);
        artistStatus = CatalogSearchLifecycleStatus.Deferred;
        artistPriority = null;
        artistEstimatedRetryAfterSeconds = @event.EstimatedRetryAfterSeconds;
        artistEarliestExpectedCompletionAt = @event.EarliestExpectedCompletionAt;
        artistReason = @event.Reason;
    }

    private void On(KnownArtistDiscoveryFailed @event)
    {
        knownItem ??= KnownCatalogItem.ForArtist(@event.ArtistId);
        artistStatus = CatalogSearchLifecycleStatus.Failed;
        artistPriority = @event.Priority;
        artistEstimatedRetryAfterSeconds = null;
        artistEarliestExpectedCompletionAt = null;
        artistReason = @event.Reason;
    }

    private void On(AlbumCatalogLookupRequested @event)
    {
        knownItem ??= KnownCatalogItem.ForAlbum(
            @event.ArtistId ?? throw new InvalidOperationException("Album lookup requests must include an artist id."),
            @event.AlbumId);
        hasAlbumCatalogLookupRequested = true;
    }

    private void On(KnownAlbumDiscoveryStarted @event)
    {
        knownItem ??= KnownCatalogItem.ForAlbum(@event.ArtistId, @event.AlbumId);
        albumStatus = CatalogSearchLifecycleStatus.InProgress;
        albumPriority = @event.Priority;
        albumEstimatedRetryAfterSeconds = null;
        albumEarliestExpectedCompletionAt = null;
        albumReason = @event.Reason;
    }

    private void On(KnownAlbumDiscoveryCompleted @event)
    {
        knownItem ??= KnownCatalogItem.ForAlbum(@event.ArtistId, @event.AlbumId);
        albumStatus = CatalogSearchLifecycleStatus.Completed;
        albumPriority = @event.Priority;
        albumEstimatedRetryAfterSeconds = null;
        albumEarliestExpectedCompletionAt = null;
        albumReason = @event.Reason;
    }

    private void On(KnownAlbumDiscoveryDeferred @event)
    {
        knownItem ??= KnownCatalogItem.ForAlbum(@event.ArtistId, @event.AlbumId);
        albumStatus = CatalogSearchLifecycleStatus.Deferred;
        albumPriority = null;
        albumEstimatedRetryAfterSeconds = @event.EstimatedRetryAfterSeconds;
        albumEarliestExpectedCompletionAt = @event.EarliestExpectedCompletionAt;
        albumReason = @event.Reason;
    }

    private void On(KnownAlbumDiscoveryFailed @event)
    {
        knownItem ??= KnownCatalogItem.ForAlbum(@event.ArtistId, @event.AlbumId);
        albumStatus = CatalogSearchLifecycleStatus.Failed;
        albumPriority = @event.Priority;
        albumEstimatedRetryAfterSeconds = null;
        albumEarliestExpectedCompletionAt = null;
        albumReason = @event.Reason;
    }
}
