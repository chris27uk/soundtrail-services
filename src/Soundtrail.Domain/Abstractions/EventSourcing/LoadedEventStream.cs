using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Abstractions.EventSourcing;

public sealed record LoadedEventStream<TStreamId>(
    TStreamId StreamId,
    int Version,
    IReadOnlyList<IDomainEvent> Events)
    where TStreamId : IValueType
{
    public static LoadedEventStream<TStreamId> Empty(TStreamId streamId) => new(streamId, 0, []);
}
