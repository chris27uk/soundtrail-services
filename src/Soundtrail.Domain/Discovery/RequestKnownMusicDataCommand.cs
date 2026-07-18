using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record RequestKnownMusicDataCommand(
    CatalogItemOperation Operation,
    LookupPriorityBand Priority,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAt) : ICommand
{
    public CommandId CommandId { get; init; } = CommandId.New();

    public CorrelationId CorrelationId { get; init; } = CorrelationId.New();

    public DateTimeOffset CreatedAt { get; init; } = RequestedAt;
}
