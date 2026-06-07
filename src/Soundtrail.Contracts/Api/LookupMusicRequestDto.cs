namespace Soundtrail.Contracts.Api;

public sealed record LookupMusicRequestDto(string Query, int TrustLevel, int RiskScore, DateTimeOffset OccurredAt, string CorrelationId);
