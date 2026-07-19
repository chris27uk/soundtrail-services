using Raven.Client.Documents.Session;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;

namespace Soundtrail.Adapters.EventSourcing;

internal sealed class RavenEventStreamRepository<TStreamId>(
    IAsyncDocumentSession session,
    ITypeRegistry typeRegistry,
    string streamName) : IEventStreamRepository<TStreamId>
    where TStreamId : IValueType
{
    private readonly RavenEventStore<TStreamId> eventStore = new(session, typeRegistry, streamName);

    public Task<LoadedEventStream<TStreamId>> LoadAsync(TStreamId streamId, CancellationToken cancellationToken) => this.eventStore.LoadAsync(streamId, cancellationToken);

    public Task<AppendResult> AppendAsync(LoadedEventStream<TStreamId> stream, IReadOnlyList<IDomainEvent> events,
        OperationId? operationId, CancellationToken cancellationToken) => this.eventStore.AppendAsync(
        stream,
        events,
        operationId,
        cancellationToken,
        saveChanges: true);
}
