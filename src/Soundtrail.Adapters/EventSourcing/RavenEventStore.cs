using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Adapters.EventSourcing;

public sealed class RavenEventStore<TStreamId, TEvent>(
    IAsyncDocumentSession session,
    ITypeRegistry typeRegistry,
    RavenEventStreamDefinition definition)
    where TStreamId : IValueType
{
    public async Task<EventStream<TEvent>> LoadAsync(
        TStreamId streamId,
        CancellationToken cancellationToken)
    {
        var metadataId = GetMetadataId(streamId);
        var metadata = await session.LoadAsync<RavenEventStreamMetadataRecord>(metadataId, cancellationToken);
        if (metadata is null)
        {
            return new EventStream<TEvent>(0, []);
        }

        var storedEvents = (await session.Advanced.LoadStartingWithAsync<RavenStoredEventRecord>(GetEventPrefix(streamId), token: cancellationToken))
            .OrderBy(x => x.Version)
            .ToList();

        return storedEvents.Count == 0
            ? new EventStream<TEvent>(metadata.Version, [])
            : new EventStream<TEvent>(
                metadata.Version,
                storedEvents.Select(ToDomainEvent).ToArray());
    }

    public async Task<AppendResult<TEvent>> AppendAsync(
        AppendRequest<TStreamId, TEvent> request,
        CancellationToken cancellationToken,
        bool saveChanges = false,
        Func<IAsyncDocumentSession, AppendRequest<TStreamId, TEvent>, CancellationToken, Task>? beforeSave = null)
    {
        session.Advanced.UseOptimisticConcurrency = true;

        var metadataId = GetMetadataId(request.StreamId);
        var metadata = await session.LoadAsync<RavenEventStreamMetadataRecord>(metadataId, cancellationToken)
            ?? CreateMetadata(request.StreamId, metadataId);

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
            .Select((@event, index) =>
                ToStoredEvent(
                    request.StreamId,
                    request.ExpectedVersion + index + 1,
                    request.OperationId,
                    @event))
            .ToArray();

        if (request.OperationId is { } newOperationId)
        {
            metadata.AppliedOperationIds.Add(newOperationId.StableValue);
        }

        metadata.Version += request.Events.Count;
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
            await beforeSave(session, request, cancellationToken);
        }

        if (saveChanges)
        {
            await session.SaveChangesAsync(cancellationToken);
        }

        return new AppendResult<TEvent>(true, metadata.Version, request.Events.ToArray(), AppendOutcome.Appended);
    }

    private RavenStoredEventRecord ToStoredEvent(
        TStreamId streamId,
        int version,
        OperationId? operationId,
        TEvent @event)
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

    private TEvent ToDomainEvent(RavenStoredEventRecord storedEvent)
    {
        if (storedEvent.Body is null)
        {
            throw new InvalidOperationException($"Stored event '{storedEvent.Id}' is missing a body.");
        }

        return (TEvent)typeRegistry.ToDomainObject(storedEvent.Body);
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
