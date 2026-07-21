namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkPriorityRaisedEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    int? TrustLevel,
    int? RiskScore,
    DateTimeOffset RequestedAtUtc,
    string CorrelationId) : RavenEventBodyDto;
