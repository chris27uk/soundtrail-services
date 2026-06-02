using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Features.Search.Queueing;

public sealed record LookupMusicRequest(
    NormalizedSearchQuery Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    string CorrelationId);
