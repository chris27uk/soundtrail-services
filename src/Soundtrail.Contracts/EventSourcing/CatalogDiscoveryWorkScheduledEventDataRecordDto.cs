namespace Soundtrail.Contracts.EventSourcing;

public sealed record CatalogDiscoveryWorkScheduledEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    DateTimeOffset NextEligibleAtUtc,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset ScheduledAtUtc) : RavenEventBodyDto;
