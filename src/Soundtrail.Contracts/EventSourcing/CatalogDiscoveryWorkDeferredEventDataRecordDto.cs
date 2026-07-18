namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkDeferredEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    DateTimeOffset NextEligibleAtUtc,
    int? EstimatedRetryAfterSeconds,
    string Reason,
    DateTimeOffset DeferredAtUtc) : RavenEventBodyDto;
