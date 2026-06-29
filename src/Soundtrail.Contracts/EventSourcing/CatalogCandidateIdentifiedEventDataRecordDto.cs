namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogCandidateIdentifiedEventDataRecordDto(
    string Criteria,
    string MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset StartedAtUtc,
    string CorrelationId) : RavenEventBodyDto;
