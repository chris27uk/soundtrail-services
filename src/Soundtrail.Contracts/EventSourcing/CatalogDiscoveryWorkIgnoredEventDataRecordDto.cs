namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkIgnoredEventDataRecordDto(
    string MusicCatalogId,
    DateTimeOffset? NextEligibleAtUtc,
    string Reason,
    DateTimeOffset IgnoredAtUtc) : RavenEventBodyDto;
