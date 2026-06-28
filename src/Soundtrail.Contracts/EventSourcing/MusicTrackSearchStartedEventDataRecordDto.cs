namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogSearchCandidateRecordedEventDataRecordDto(
    string Criteria,
    string MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset StartedAtUtc,
    string CorrelationId) : RavenEventBodyDto;
