using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Abstractions.EventSourcing;

public interface IEventStreamRepository<TStreamId, TEvent>
    where TStreamId : IValueType
{
    Task<EventStream<TEvent>> LoadAsync(
        TStreamId streamId,
        CancellationToken cancellationToken);

    Task<AppendResult<TEvent>> AppendAsync(
        AppendRequest<TStreamId, TEvent> request,
        CancellationToken cancellationToken);
}
