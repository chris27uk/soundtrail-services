namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record CatalogSearchAttemptDto(string Criteria, string Query, string Playback, int TrustLevel, int RiskScore, DateTimeOffset OccurredAt, string CorrelationId);
