namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkRequestedEventDataRecordDto(
    string ResourceKind,
    string ResourceValue,
    string? ResourceItemKind,
    string Priority,
    int? TrustLevel,
    int? RiskScore,
    DateTimeOffset RequestedAtUtc,
    string CorrelationId) : RavenEventBodyDto;
