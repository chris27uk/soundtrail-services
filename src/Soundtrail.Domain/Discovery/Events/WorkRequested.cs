using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events
{
    public sealed record WorkRequested(
        EnrichmentTarget Target,
        int? TrustLevel,
        int? RiskScore,
        DateTimeOffset RequestedAt,
        CorrelationId CorrelationId) : IDomainEvent
    {
        public CommandId SubsequentDeterministicId(string command) => CommandId.For($"{command}:{this.Target.NormalisedIdentifier}:{this.TrustLevel}:{this.RiskScore}:{this.CorrelationId.Value}");
    }
}
