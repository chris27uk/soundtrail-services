using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Domain.Discovery;

public sealed record SearchForCatalogItemsCommand(
    EnrichmentTarget Target,
    RequiredCatalogType RequiredCatalogType,
    LookupPriorityBand Priority,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAt) : ICommand
{
    public CommandId CommandId { get; init; } = CommandId.New();

    public CorrelationId CorrelationId { get; init; } = CorrelationId.New();

    public DateTimeOffset CreatedAt { get; init; } = RequestedAt;
}
