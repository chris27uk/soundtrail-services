namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkRequestedEventDataRecordDto(
    string MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAtUtc);
