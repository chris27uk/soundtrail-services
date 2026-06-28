using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Abstractions.EventSourcing;

public sealed record LoadedEventStream<TStreamId, TEvent>(
    TStreamId StreamId,
    int Version,
    IReadOnlyList<TEvent> Events)
    where TStreamId : IValueType
{
    public static LoadedEventStream<TStreamId, TEvent> Empty(TStreamId streamId) =>
        new(streamId, 0, []);
}
