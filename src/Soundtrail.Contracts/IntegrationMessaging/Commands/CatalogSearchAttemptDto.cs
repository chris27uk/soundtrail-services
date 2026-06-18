namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record CatalogSearchAttemptDto(string Criteria, string Query, int TrustLevel, int RiskScore, DateTimeOffset OccurredAt, string CorrelationId);
