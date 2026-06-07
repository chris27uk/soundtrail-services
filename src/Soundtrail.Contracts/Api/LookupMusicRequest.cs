namespace Soundtrail.Contracts.Api;

public sealed record LookupMusicRequest(string Query, int TrustLevel, int RiskScore, DateTimeOffset OccurredAt, string CorrelationId);
