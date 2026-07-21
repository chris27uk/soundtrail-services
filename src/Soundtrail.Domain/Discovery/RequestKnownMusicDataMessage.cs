using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record RequestKnownMusicDataMessage(
    CatalogItemOperation Operation,
    LookupPriorityBand Priority,
    int? TrustLevel,
    int? RiskScore,
    DateTimeOffset RequestedAt) : IPrioritisedMessage
{
    public MessageId Id { get; init; } = MessageId.New();

    public CorrelationId CorrelationId { get; init; } = CorrelationId.New();

    public DateTimeOffset CreatedAt { get; init; } = RequestedAt;
}
