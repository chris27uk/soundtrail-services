using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Enrichment;

public sealed class MusicCatalogLookupHistory
{
    private readonly List<IDomainEvent> uncommittedEvents = [];
    private readonly EventHandlers<MusicCatalogLookupHistory> eventHandlers;
    private MusicCatalogId? musicCatalogId;

    private MusicCatalogLookupHistory(
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

    private MusicCatalogLookupHistory(IEnumerable<IDomainEvent> events)
    {
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<(LoadedEventStream<MusicCatalogLookupId, IDomainEvent> Stream, MusicCatalogLookupHistory Aggregate)> LoadAsync(
        IEventStreamRepository<MusicCatalogLookupId, IDomainEvent> repository,
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(MusicCatalogLookupId.From(musicCatalogId), cancellationToken);
        return (stream, new MusicCatalogLookupHistory(musicCatalogId, stream.Events));
    }

    public static MusicCatalogLookupHistory Replay(IEnumerable<IDomainEvent> events) =>
        new(events);

    public void ApplyToKnownTrackDiscovery(KnownItemDiscovery discovery, IDomainEvent @event)
    {
        switch (@event)
        {
            case MusicCatalogLookupStarted started:
                discovery.TrackLookupStarted(
                    TrackId.From(started.MusicCatalogId.Value),
                    started.Priority,
                    "Lookup started",
                    started.StartedAt);
                break;
            case MusicCatalogLookupCompleted completed:
                discovery.TrackLookupCompleted(
                    TrackId.From(completed.MusicCatalogId.Value),
                    completed.Priority,
                    "Discovery completed",
                    completed.CompletedAt);
                break;
            case MusicCatalogLookupDeferred deferred:
                discovery.TrackLookupDeferred(
                    TrackId.From(deferred.MusicCatalogId.Value),
                    deferred.RetryAfterSeconds,
                    deferred.RetryAt,
                    deferred.Reason,
                    deferred.DeferredAt);
                break;
            case MusicCatalogLookupFailed failed:
                discovery.TrackLookupStarted(
                    TrackId.From(failed.MusicCatalogId.Value),
                    failed.Priority,
                    "Lookup started",
                    failed.FailedAt);
                discovery.TrackLookupFailed(
                    TrackId.From(failed.MusicCatalogId.Value),
                    failed.Priority,
                    failed.Reason,
                    failed.FailedAt);
                break;
        }
    }

    public void ApplyToSearchDiscovery(SearchDiscoveryHistory discovery, IDomainEvent @event)
    {
        switch (@event)
        {
            case MusicCatalogLookupStarted started:
                _ = discovery.LookupStarted(started.Priority, started.StartedAt);
                break;
            case MusicCatalogLookupCompleted completed:
                _ = discovery.LookupCompleted(completed.Priority, completed.CompletedAt);
                break;
            case MusicCatalogLookupDeferred deferred:
                discovery.LookupDeferred(
                    deferred.RetryAfterSeconds,
                    deferred.RetryAt,
                    deferred.Reason,
                    deferred.DeferredAt);
                break;
            case MusicCatalogLookupFailed failed:
                _ = discovery.LookupFailed(
                    failed.Priority,
                    failed.Reason,
                    failed.FailedAt);
                break;
        }
    }

    public IReadOnlyList<MusicSearchCriteria> ResolveSearchTerms(
        IDomainEvent @event,
        IReadOnlyList<MusicSearchCriteria> fallbackSearchTerms)
    {
        return @event switch
        {
            MusicCatalogLookupCompleted completed when completed.Metadata is not null =>
                MergeSearchTerms(
                    MusicSearchTermSet.ForResolvedTrack(
                        completed.MusicCatalogId,
                        completed.Hierarchy?.ArtistId,
                        completed.Hierarchy?.AlbumId,
                        completed.SearchCriteria),
                    fallbackSearchTerms),
            MusicCatalogLookupCompleted completed => completed.SearchCriteria is not null
                ? [completed.SearchCriteria]
                : fallbackSearchTerms,
            MusicCatalogLookupDeferred deferred => deferred.SearchCriteria is not null
                ? [deferred.SearchCriteria]
                : fallbackSearchTerms,
            MusicCatalogLookupFailed failed => failed.SearchCriteria is not null
                ? [failed.SearchCriteria]
                : fallbackSearchTerms,
            _ => fallbackSearchTerms
        };
    }

    private static IReadOnlyList<MusicSearchCriteria> MergeSearchTerms(
        IReadOnlyList<MusicSearchCriteria> primary,
        IReadOnlyList<MusicSearchCriteria> fallback)
    {
        var values = new HashSet<MusicSearchCriteria>();
        var merged = new List<MusicSearchCriteria>();

        foreach (var item in primary)
        {
            if (values.Add(item))
            {
                merged.Add(item);
            }
        }

        foreach (var item in fallback)
        {
            if (values.Add(item))
            {
                merged.Add(item);
            }
        }

        return merged;
    }

    public bool Record(MusicCatalogLookupAttempted attempted)
    {
        return attempted.MusicCatalogMetadataFetched is not null
            ? RecordCompleted(attempted)
            : attempted.Outcome.Status switch
            {
                MusicCatalogLookupOutcomeStatus.Deferred => RecordDeferred(attempted),
                MusicCatalogLookupOutcomeStatus.Failed => RecordFailed(attempted),
                MusicCatalogLookupOutcomeStatus.Duplicate => false,
                _ => RecordStarted(attempted)
            };
    }

    public async Task SaveAsync(
        IEventStreamRepository<MusicCatalogLookupId, IDomainEvent> repository,
        LoadedEventStream<MusicCatalogLookupId, IDomainEvent> stream,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        var append = await repository.AppendAsync(
            stream,
            uncommittedEvents.AsReadOnly(),
            OperationId.From(commandId.Value),
            cancellationToken);

        if (append.Outcome == AppendOutcome.VersionMismatch)
        {
            throw new InvalidOperationException($"Lookup history stream concurrency conflict for '{musicCatalogId?.Value}'.");
        }

        if (append.Appended)
        {
            uncommittedEvents.Clear();
        }
    }

    private bool RecordStarted(MusicCatalogLookupAttempted attempted)
    {
        Apply(
            new MusicCatalogLookupStarted(
                attempted.MusicCatalogId,
                attempted.Priority,
                attempted.CreatedAt),
            isNew: true);
        return true;
    }

    private bool RecordCompleted(MusicCatalogLookupAttempted attempted)
    {
        var fetched = attempted.MusicCatalogMetadataFetched
                     ?? throw new InvalidOperationException("Completed attempt must include fetched metadata.");

        Apply(
            new MusicCatalogLookupCompleted(
                fetched.MusicCatalogId,
                fetched.SourceProvider,
                fetched.Priority,
                fetched.CreatedAt,
                fetched.Metadata,
                fetched.References,
                fetched.FailedProviders,
                fetched.Hierarchy,
                attempted.SearchCriteria),
            isNew: true);
        return true;
    }

    private bool RecordDeferred(MusicCatalogLookupAttempted attempted)
    {
        Apply(
            new MusicCatalogLookupDeferred(
                attempted.MusicCatalogId,
                attempted.Priority,
                attempted.CreatedAt,
                attempted.Outcome.RetryAfterSeconds,
                attempted.Outcome.RetryAt,
                attempted.Outcome.Reason ?? "Lookup deferred",
                attempted.SearchCriteria),
            isNew: true);
        return true;
    }

    private bool RecordFailed(MusicCatalogLookupAttempted attempted)
    {
        Apply(
            new MusicCatalogLookupFailed(
                attempted.MusicCatalogId,
                attempted.Priority,
                attempted.CreatedAt,
                attempted.Outcome.Reason ?? "Lookup failed",
                attempted.SearchCriteria),
            isNew: true);
        return true;
    }

    private void Apply(IDomainEvent @event, bool isNew)
    {
        eventHandlers.Handle(@event);

        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
    }

    private EventHandlers<MusicCatalogLookupHistory> CreateHandlers()
    {
        var handlers = new EventHandlers<MusicCatalogLookupHistory>();
        handlers.Register<MusicCatalogLookupStarted>(@event => musicCatalogId = @event.MusicCatalogId);
        handlers.Register<MusicCatalogLookupCompleted>(@event => musicCatalogId = @event.MusicCatalogId);
        handlers.Register<MusicCatalogLookupDeferred>(@event => musicCatalogId = @event.MusicCatalogId);
        handlers.Register<MusicCatalogLookupFailed>(@event => musicCatalogId = @event.MusicCatalogId);
        return handlers;
    }
}
