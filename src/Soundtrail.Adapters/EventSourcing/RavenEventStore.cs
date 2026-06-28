using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;

namespace Soundtrail.Adapters.EventSourcing;

public sealed class RavenEventStore<TStreamId, TEvent, TStoredEvent, TMetadata>(
    IAsyncDocumentSession session,
    Func<TStreamId, string> metadataIdFactory,
    Func<TStreamId, string, TMetadata> createMetadata,
    Func<TStreamId, string> eventPrefixFactory,
    Func<TStreamId, int, OperationId?, TEvent, TStoredEvent> toStoredEvent,
    Func<TStoredEvent, TEvent> toDomainEvent,
    Func<TStoredEvent, DateTimeOffset> occurredAtSelector,
    Func<TStoredEvent, int> versionSelector)
    where TStreamId : IValueType
    where TMetadata : class, IEventStreamMetadataRecord
{
    public async Task<EventStream<TEvent>> LoadAsync(
        TStreamId streamId,
        CancellationToken cancellationToken)
    {
        var metadataId = metadataIdFactory(streamId);
        var metadata = await session.LoadAsync<TMetadata>(metadataId, cancellationToken);
        if (metadata is null)
        {
            return new EventStream<TEvent>(0, []);
        }

        var storedEvents = (await session.Advanced.LoadStartingWithAsync<TStoredEvent>(eventPrefixFactory(streamId)))
            .OrderBy(versionSelector)
            .ToList();

        return storedEvents.Count == 0
            ? new EventStream<TEvent>(metadata.Version, [])
            : new EventStream<TEvent>(metadata.Version, storedEvents.Select(toDomainEvent).ToArray());
    }

    public async Task<AppendResult<TEvent>> AppendAsync(
        AppendRequest<TStreamId, TEvent> request,
        CancellationToken cancellationToken,
        bool saveChanges = false,
        Func<IAsyncDocumentSession, AppendRequest<TStreamId, TEvent>, CancellationToken, Task>? beforeSave = null)
    {
        session.Advanced.UseOptimisticConcurrency = true;

        var metadataId = metadataIdFactory(request.StreamId);
        var metadata = await session.LoadAsync<TMetadata>(metadataId, cancellationToken)
            ?? createMetadata(request.StreamId, metadataId);

        if (request.OperationId is { } operationId &&
            metadata.AppliedOperationIds.Contains(operationId.StableValue))
        {
            return new AppendResult<TEvent>(false, metadata.Version, [], AppendOutcome.DuplicateOperation);
        }

        if (metadata.Version != request.ExpectedVersion)
        {
            return new AppendResult<TEvent>(false, metadata.Version, [], AppendOutcome.VersionMismatch);
        }

        var storedEvents = request.Events
            .Select((@event, index) => toStoredEvent(request.StreamId, request.ExpectedVersion + index + 1, request.OperationId, @event))
            .ToArray();

        if (request.OperationId is { } newOperationId)
        {
            metadata.AppliedOperationIds.Add(newOperationId.StableValue);
        }

        metadata.Version += request.Events.Count;
        metadata.UpdatedAtUtc = storedEvents.Length == 0
            ? DateTimeOffset.UtcNow
            : storedEvents.Max(occurredAtSelector);

        await session.StoreAsync(metadata, cancellationToken);

        foreach (var storedEvent in storedEvents)
        {
            await session.StoreAsync(storedEvent, cancellationToken);
        }

        if (beforeSave is not null)
        {
            await beforeSave(session, request, cancellationToken);
        }

        if (saveChanges)
        {
            await session.SaveChangesAsync(cancellationToken);
        }

        return new AppendResult<TEvent>(true, metadata.Version, request.Events.ToArray(), AppendOutcome.Appended);
    }
}
