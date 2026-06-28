namespace Soundtrail.Contracts.EventSourcing;

public sealed record DiscoveryPlannedEventDataRecordDto(
    string Criteria,
    string Priority,
    bool WillBeLookedUp,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset PlannedAtUtc) : RavenEventBodyDto;
