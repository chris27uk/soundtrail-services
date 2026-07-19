using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages;

public sealed record AssessWorkCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    int? TrustLevel = null,
    int? RiskScore = null) : IPrioritisedCommand
{
    public DateTimeOffset RequestedAt => CreatedAt;

    public static CommandId Id(EnrichmentTarget target, DateTimeOffset createdAt) => CommandId.For($"AssessWork:{target.NormalisedIdentifier}:{createdAt}");
}
