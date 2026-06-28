namespace Soundtrail.Contracts.EventSourcing;

public sealed record TrackMetadataLookupRequestedEventDataRecordDto(
    string Criteria,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequiredAtUtc,
    string CorrelationId) : RavenEventBodyDto;
