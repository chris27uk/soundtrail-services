using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Abstractions.EventSourcing;

public sealed record AppendRequest<TStreamId, TEvent>(
    TStreamId StreamId,
    int ExpectedVersion,
    IReadOnlyList<TEvent> Events,
    OperationId? OperationId = null)
    where TStreamId : IValueType;
