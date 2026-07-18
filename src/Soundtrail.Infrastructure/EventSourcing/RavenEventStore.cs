using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Adapters.EventSourcing;

internal sealed class RavenEventStore<TStreamId>(
    IAsyncDocumentSession session,
    ITypeRegistry typeRegistry,
    RavenEventStreamDefinition definition)
    where TStreamId : IValueType
{
    public async Task<LoadedEventStream<TStreamId>> LoadAsync(
        TStreamId streamId,
        CancellationToken cancellationToken)
    {
        var metadataId = GetMetadataId(streamId);
        var metadata = await session.LoadAsync<RavenEventStreamMetadataRecord>(metadataId, cancellationToken);
        if (metadata is null)
        {
            return LoadedEventStream<TStreamId>.Empty(streamId);
        }

        var storedEvents = (await session.Advanced.LoadStartingWithAsync<RavenStoredEventRecord>(GetEventPrefix(streamId), token: cancellationToken))
            .OrderBy(x => x.Version)
            .ToList();

        return storedEvents.Count == 0
            ? new LoadedEventStream<TStreamId>(streamId, metadata.Version, [])
            : new LoadedEventStream<TStreamId>(
                streamId,
                metadata.Version,
                storedEvents.Select(ToDomainEvent).ToArray());
    }

    public async Task<AppendResult> AppendAsync(
        LoadedEventStream<TStreamId> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken,
        bool saveChanges = false,
        Func<IAsyncDocumentSession, LoadedEventStream<TStreamId>, IReadOnlyList<IDomainEvent>, OperationId?, CancellationToken, Task>? beforeSave = null)
    {
        session.Advanced.UseOptimisticConcurrency = true;

        var metadataId = GetMetadataId(stream.StreamId);
        var metadata = await session.LoadAsync<RavenEventStreamMetadataRecord>(metadataId, cancellationToken)
            ?? CreateMetadata(stream.StreamId, metadataId);

        if (operationId is { } duplicateCheckOperationId &&
            metadata.AppliedOperationIds.Contains(duplicateCheckOperationId.StableValue))
        {
            return new AppendResult(false, metadata.Version, [], AppendOutcome.DuplicateOperation);
        }

        if (metadata.Version != stream.Version)
        {
            return new AppendResult(false, metadata.Version, [], AppendOutcome.VersionMismatch);
        }

        var storedEvents = events
            .Select((@event, index) =>
                ToStoredEvent(
                    stream.StreamId,
                    stream.Version + index + 1,
                    operationId,
                    @event))
            .ToArray();

        if (operationId is { } newOperationId)
        {
            metadata.AppliedOperationIds.Add(newOperationId.StableValue);
        }

        metadata.Version += events.Count;
        metadata.UpdatedAtUtc = storedEvents.Length == 0
            ? DateTimeOffset.UtcNow
            : storedEvents.Max(x => x.OccurredAtUtc);

        await session.StoreAsync(metadata, cancellationToken);

        foreach (var storedEvent in storedEvents)
        {
            await session.StoreAsync(storedEvent, cancellationToken);
        }

        if (beforeSave is not null)
        {
            await beforeSave(session, stream, events, operationId, cancellationToken);
        }

        if (saveChanges)
        {
            await session.SaveChangesAsync(cancellationToken);
        }

        return new AppendResult(true, metadata.Version, events.ToArray(), AppendOutcome.Appended);
    }

    private RavenStoredEventRecord ToStoredEvent(
        TStreamId streamId,
        int version,
        OperationId? operationId,
        IDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var registry = (TypeTranslationRegistry)typeRegistry;
        var registration = registry.GetStoredEventRegistrationForDomain(@event.GetType());
        var body = (RavenEventBodyDto)typeRegistry.ToDto(@event!);

        return new RavenStoredEventRecord
        {
            Id = GetEventId(streamId, version),
            StreamId = streamId.StableValue,
            AggregateType = definition.StreamName,
            Version = version,
            EventId = $"{streamId.StableValue}:{version:D10}",
            EventType = registration.EventType,
            BodyType = registration.DtoType.FullName ?? registration.DtoType.Name,
            Body = body,
            OccurredAtUtc = registration.OccurredAtUtc(@event!),
            CorrelationId = registration.CorrelationId(@event!),
            CausationId = operationId?.StableValue
        };
    }

    private IDomainEvent ToDomainEvent(RavenStoredEventRecord storedEvent)
    {
        if (storedEvent.Body is null)
        {
            throw new InvalidOperationException($"Stored event '{storedEvent.Id}' is missing a body.");
        }

        return (IDomainEvent)typeRegistry.ToDomainObject(storedEvent.Body);
    }

    private RavenEventStreamMetadataRecord CreateMetadata(TStreamId streamId, string metadataId) =>
        new()
        {
            Id = metadataId,
            StreamId = streamId.StableValue,
            AggregateType = definition.StreamName
        };

    private string GetMetadataId(TStreamId streamId) =>
        $"{definition.StreamName}-streams/{streamId.StableValue}";

    private string GetEventPrefix(TStreamId streamId) =>
        $"{definition.StreamName}-events/{streamId.StableValue}/";

    private string GetEventId(TStreamId streamId, int version) =>
        $"{GetEventPrefix(streamId)}{version:D10}";
}
