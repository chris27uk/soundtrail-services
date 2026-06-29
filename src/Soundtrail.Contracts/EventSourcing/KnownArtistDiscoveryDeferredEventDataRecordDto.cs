namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownArtistDiscoveryDeferredEventDataRecordDto(
    string ArtistId,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset DeferredAtUtc) : RavenEventBodyDto;
