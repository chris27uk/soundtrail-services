namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkDeferredEventDataRecordDto(
    string MusicCatalogId,
    DateTimeOffset NextEligibleAtUtc,
    string Reason,
    DateTimeOffset DeferredAtUtc);
