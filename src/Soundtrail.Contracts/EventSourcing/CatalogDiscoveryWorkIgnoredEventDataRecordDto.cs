namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkIgnoredEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    DateTimeOffset? NextEligibleAtUtc,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset IgnoredAtUtc) : RavenEventBodyDto;
