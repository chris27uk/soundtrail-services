using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Abstractions.EventSourcing;

public interface IEventStreamRepository<TStreamId>
    where TStreamId : IValueType
{
    Task<LoadedEventStream<TStreamId>> LoadAsync(
        TStreamId streamId,
        CancellationToken cancellationToken);

    Task<AppendResult> AppendAsync(
        LoadedEventStream<TStreamId> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken);
}
