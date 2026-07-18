using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Adapters.EventSourcing;

internal sealed class RavenEventStreamRepository<TStreamId>(
    IAsyncDocumentSession session,
    ITypeRegistry typeRegistry,
    RavenEventStreamDefinition definition) : IEventStreamRepository<TStreamId>
    where TStreamId : IValueType
{
    private readonly RavenEventStore<TStreamId> eventStore =
        new(
            session,
            typeRegistry,
            definition);

    public Task<LoadedEventStream<TStreamId>> LoadAsync(
        TStreamId streamId,
        CancellationToken cancellationToken) =>
        eventStore.LoadAsync(streamId, cancellationToken);

    public Task<AppendResult> AppendAsync(
        LoadedEventStream<TStreamId> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken) =>
        eventStore.AppendAsync(
            stream,
            events,
            operationId,
            cancellationToken,
            saveChanges: true);
}
