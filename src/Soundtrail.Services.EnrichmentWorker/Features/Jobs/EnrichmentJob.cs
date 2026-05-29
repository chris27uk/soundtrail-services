using Soundtrail.Services.EnrichmentWorker.Models;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.EnrichmentWorker.Jobs;

public sealed record EnrichmentJob(
    string JobId,
    QueryId QueryId,
    NormalizedSearchQuery NormalizedQuery,
    EnrichmentStage Stage,
    ProviderName Provider,
    int PriorityScore,
    int Attempts,
    DateTimeOffset NotBefore,
    DateTimeOffset CreatedAt,
    string CorrelationId);
