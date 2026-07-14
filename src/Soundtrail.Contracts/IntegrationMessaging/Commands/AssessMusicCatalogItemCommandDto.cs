using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record AssessMusicCatalogItemCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBandDto Priority,
    CatalogItemKindDto ItemKindDto,
    string ItemValue,
    CatalogItemResourceKindDto ResourceKindDto,
    string ResourceValue,
    CatalogItemKindDto? ResourceItemKind,
    int? TrustLevel,
    int? RiskScore);
