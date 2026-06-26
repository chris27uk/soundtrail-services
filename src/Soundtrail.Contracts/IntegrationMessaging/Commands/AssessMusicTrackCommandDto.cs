using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record AssessMusicTrackCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    string MusicCatalogId,
    string? Criteria,
    int? TrustLevel,
    int? RiskScore);
