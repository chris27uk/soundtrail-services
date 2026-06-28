using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Abstractions.EventSourcing;

public interface IEventStreamRepository<TStreamId, TEvent>
    where TStreamId : IValueType
{
    Task<LoadedEventStream<TStreamId, TEvent>> LoadAsync(
        TStreamId streamId,
        CancellationToken cancellationToken);

    Task<AppendResult<TEvent>> AppendAsync(
        LoadedEventStream<TStreamId, TEvent> stream,
        IReadOnlyList<TEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken);
}
