namespace Soundtrail.Contracts.EventSourcing;

public sealed record MusicMetadataRequiredEventDataRecordDto(
    string Criteria,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequiredAtUtc,
    string CorrelationId);
