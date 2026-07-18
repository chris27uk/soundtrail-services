using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Events
{
    public sealed record WorkRequested(
        EnrichmentTarget Target,
        LookupPriorityBand Priority,
        int? TrustLevel,
        int? RiskScore,
        DateTimeOffset RequestedAt,
        CorrelationId CorrelationId) : IDomainEvent
    {
        public CommandId SubsequentDeterministicId(string command) => CommandId.For($"{command}:{this.Target.NormalisedIdentifier}:{this.TrustLevel}:{this.RiskScore}:{this.CorrelationId.Value}");
    }
}
