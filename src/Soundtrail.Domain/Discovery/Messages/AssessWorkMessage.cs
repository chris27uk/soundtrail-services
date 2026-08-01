using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Messages;

public sealed record AssessWorkMessage(
    MessageId Id,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    int? TrustLevel = null,
    int? RiskScore = null) : IPrioritisedMessage, ITargetedMessage
{
    public DateTimeOffset RequestedAt => CreatedAt;

    public static MessageId NewId(EnrichmentTarget target, DateTimeOffset createdAt) =>
        MessageId.Deterministic("AssessWork", target.NormalisedIdentifier, createdAt.ToString("O"));
}
