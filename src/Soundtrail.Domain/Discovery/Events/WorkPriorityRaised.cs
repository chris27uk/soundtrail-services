using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record WorkPriorityRaised(
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    int? TrustLevel,
    int? RiskScore,
    DateTimeOffset RequestedAt,
    CorrelationId CorrelationId) : IDomainEvent
{
    public MessageId SubsequentDeterministicId(string command) =>
        MessageId.Deterministic(
            command,
            Target.NormalisedIdentifier,
            TrustLevel?.ToString(),
            RiskScore?.ToString(),
            CorrelationId.Value);
}
