using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record LookupExecutionReportDto(
    string CommandId,
    string MusicCatalogId,
    string SourceProvider,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    string CorrelationId,
    string Outcome,
    string? Reason,
    DateTimeOffset? RetryAt,
    int? RetryAfterSeconds);
