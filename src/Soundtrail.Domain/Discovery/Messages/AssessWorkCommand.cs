using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Assesment;

public sealed record AssessWorkCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    int? TrustLevel = null,
    int? RiskScore = null) : ICommand
{
    public static CommandId Id(EnrichmentTarget target, DateTimeOffset createdAt) => CommandId.For($"AssessWork:{target.NormalisedIdentifier}:{createdAt}");
}
