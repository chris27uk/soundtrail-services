using Soundtrail.Services.Enrichment.Jobs;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Enrichment.Models;

public sealed record EnrichmentAttempt(
    string AttemptId,
    QueryId QueryId,
    EnrichmentStage Stage,
    ProviderName Provider,
    int AttemptNumber,
    EnrichmentOutcome Outcome,
    string? ErrorCode,
    int? ProviderStatusCode,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    DateTimeOffset? NextRetryAt);
