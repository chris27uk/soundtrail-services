namespace Soundtrail.Contracts.EventSourcing;

public sealed record DiscoveryRequestedEventDataRecordDto(
    string Criteria,
    string Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAtUtc,
    string CorrelationId) : RavenEventBodyDto;
