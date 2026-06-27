namespace Soundtrail.Contracts.EventSourcing;

public sealed record MusicTrackSearchStartedEventDataRecordDto(
    string Criteria,
    string MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset StartedAtUtc,
    string CorrelationId);
