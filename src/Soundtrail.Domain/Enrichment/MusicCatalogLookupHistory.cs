using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Enrichment.Responses;

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

    public static async Task<(LoadedEventStream<MusicCatalogLookupId, IDomainEvent> Stream, MusicCatalogLookupHistory Aggregate)> LoadAsync(
        IEventStreamRepository<MusicCatalogLookupId, IDomainEvent> repository,
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(MusicCatalogLookupId.From(musicCatalogId), cancellationToken);
        return (stream, new MusicCatalogLookupHistory(musicCatalogId, stream.Events));
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
