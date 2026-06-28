using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Adapters.EventSourcing;

public sealed class RavenEventStreamRepository<TStreamId, TEvent>(
    IAsyncDocumentSession session,
    ITypeRegistry typeRegistry,
    RavenEventStreamDefinition definition) : IEventStreamRepository<TStreamId, TEvent>
    where TStreamId : IValueType
{
    private readonly RavenEventStore<TStreamId, TEvent> eventStore =
        new(
            session,
            typeRegistry,
            definition);

    public Task<EventStream<TEvent>> LoadAsync(
        TStreamId streamId,
        CancellationToken cancellationToken) =>
        eventStore.LoadAsync(streamId, cancellationToken);

    public Task<AppendResult<TEvent>> AppendAsync(
        AppendRequest<TStreamId, TEvent> request,
        CancellationToken cancellationToken) =>
        eventStore.AppendAsync(
            request,
            cancellationToken,
            saveChanges: true);
}
