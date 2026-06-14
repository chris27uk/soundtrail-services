namespace Soundtrail.Contracts.EventSourcing;

public sealed record DiscoveryRequestedEventDataRecordDto(
    string QueryKey,
    string Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAtUtc,
    string CorrelationId);
