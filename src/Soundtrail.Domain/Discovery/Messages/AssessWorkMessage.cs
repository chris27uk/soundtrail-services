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
    int? RiskScore = null) : IPrioritisedMessage
{
    public DateTimeOffset RequestedAt => CreatedAt;

    public static MessageId NewId(EnrichmentTarget target, DateTimeOffset createdAt) => MessageId.For($"AssessWork:{target.NormalisedIdentifier}:{createdAt}");
}
