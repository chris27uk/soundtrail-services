using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages;

public record DispatchLookupWork(
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt) : ICommand
{
    public DateTimeOffset RequestedAt => CreatedAt;
}
