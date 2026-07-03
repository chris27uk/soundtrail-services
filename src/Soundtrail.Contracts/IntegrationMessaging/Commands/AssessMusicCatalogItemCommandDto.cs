using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record AssessMusicCatalogItemCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    CatalogItemKind ItemKind,
    string ItemValue,
    CatalogItemResourceKind ResourceKind,
    string ResourceValue,
    CatalogItemKind? ResourceItemKind,
    int? TrustLevel,
    int? RiskScore);
