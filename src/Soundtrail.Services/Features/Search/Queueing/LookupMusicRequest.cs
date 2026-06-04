using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Features.Search.Queueing;

public sealed record LookupMusicRequest(
    NormalizedSearchQuery Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);
