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
        MessageId.For($"{command}:{Target.NormalisedIdentifier}:{TrustLevel}:{RiskScore}:{CorrelationId.Value}");
}
