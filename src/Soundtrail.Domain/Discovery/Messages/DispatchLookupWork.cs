using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages;

public record DispatchLookupWork(
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    MessageId Id,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt) : IMessage
{
    public DateTimeOffset RequestedAt => CreatedAt;
}
