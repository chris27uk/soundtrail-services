namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownTrackDiscoveryDeferredEventDataRecordDto(
    string TrackId,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset DeferredAtUtc) : RavenEventBodyDto;
