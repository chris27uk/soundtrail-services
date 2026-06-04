using Soundtrail.Services.Features.Search.TrackSearch;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Features.Search.Queueing;

public sealed record LookupMusicRequest(
    NormalizedSearchQuery Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);
