using Soundtrail.Contracts;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

public sealed record LookupMusicRequest(
    NormalizedSearchQuery Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);
