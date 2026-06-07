namespace Soundtrail.Services.Api.Features.Search.Queueing;

public sealed record LookupMusicRequest(
    string Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    string CorrelationId);
