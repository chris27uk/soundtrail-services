using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record LookupMusicRequest(
    NormalizedSearchQuery Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    string CorrelationId);
