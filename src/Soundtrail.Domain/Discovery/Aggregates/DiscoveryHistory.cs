using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
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
        this.eventHandlers.Register<ArtistDiscovered>(_ => { });
        this.eventHandlers.Register<AlbumDiscovered>(_ => { });
        this.eventHandlers.Register<TrackDiscovered>(_ => { });
        this.eventHandlers.Register<StreamingLocationDiscovered>(_ => { });
        this.eventHandlers.Register<PlaylistTracksDiscovered>(_ => { });
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

    public void Complete(
        EnrichmentTarget target,
        LookupPriorityBand priority,
        string reason,
        DateTimeOffset completedAt)
    {
        Apply(new WorkCompleted(target, priority, reason, completedAt), isNew: true);
    }

    public void DeferResult(
        EnrichmentTarget target,
        LookupPriorityBand priority,
        DateTimeOffset nextEligibleAt,
        string reason,
        DateTimeOffset deferredAt)
    {
        Apply(
            new WorkDeferred(
                target,
                priority,
                nextEligibleAt,
                EstimateRetryAfterSeconds(nextEligibleAt, deferredAt),
                reason,
                deferredAt),
            isNew: true);
    }

    public void FailAttempt(
        EnrichmentTarget target,
        string reason,
        DateTimeOffset failedAt)
    {
        Apply(new WorkAttemptFailed(target, reason, failedAt), isNew: true);
    }

    public void Discover(
        CatalogDiscoveryEntry entry,
        DateTimeOffset observedAt)
    {
        IDomainEvent @event = entry.Item switch
        {
            CatalogItem.MusicArtist(var artist) => new ArtistDiscovered(artist, observedAt),
            CatalogItem.MusicAlbum(var album) => new AlbumDiscovered(album, observedAt),
            CatalogItem.MusicTrack(var track) => new TrackDiscovered(
                track,
                new CatalogTrackHierarchy(
                    entry.ArtistId,
                    string.IsNullOrWhiteSpace(track.AlbumId)
                        ? null
                        : Soundtrail.Domain.Catalog.Albums.AlbumId.From(track.AlbumId)),
                observedAt),
            CatalogItem.MusicPlaylist => throw new InvalidOperationException("Playlist catalog items are not supported in discovery history."),
            _ => throw new InvalidOperationException($"Unsupported catalog item '{entry.Item.GetType().Name}'.")
        };

        Apply(@event, isNew: true);
    }

    public void DiscoverStreamingLocation(
        ArtistId artistId,
        TrackId trackId,
        StreamingLocation streamingLocation,
        DateTimeOffset observedAt)
    {
        Apply(
            new StreamingLocationDiscovered(
                new CatalogItemId.Track(trackId),
                new CatalogTrackHierarchy(artistId, null),
                streamingLocation.Provider,
                streamingLocation.ExternalId,
                streamingLocation.Url,
                streamingLocation.SourceProvider,
                observedAt),
            isNew: true);
    }

    public void DiscoverPlaylistTracks(
        PlaylistId playlistId,
        TrackId[] tracks,
        DateTimeOffset observedAt)
    {
        Apply(new PlaylistTracksDiscovered(playlistId, tracks, observedAt), isNew: true);
    }

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
            OperationId.From(this.requestContext.CommandId.Value),
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

    private static int EstimateRetryAfterSeconds(DateTimeOffset nextEligibleAt, DateTimeOffset observedAt)
    {
        var delay = nextEligibleAt - observedAt;
        return Math.Max(0, (int)Math.Ceiling(delay.TotalSeconds));
    }

    public sealed record SearchRequestContext(
        CommandId CommandId,
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
