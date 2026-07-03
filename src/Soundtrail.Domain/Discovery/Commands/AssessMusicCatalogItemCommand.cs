using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record AssessMusicCatalogItemCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    CatalogItemId ItemId,
    CatalogItemResource Resource,
    int? TrustLevel = null,
    int? RiskScore = null) : ICommand
{
    public static CommandId Id(
        CatalogItemId itemId,
        CatalogItemResource resource,
        DateTimeOffset createdAt) =>
        CommandId.For($"AssessMusicCatalogItem:{itemId.EntityKind}:{itemId.StableValue}:{resource.StableValue}:{createdAt.ToUnixTimeMilliseconds()}");
}
