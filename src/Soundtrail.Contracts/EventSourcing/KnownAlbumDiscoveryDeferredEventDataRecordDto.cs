namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownAlbumDiscoveryDeferredEventDataRecordDto(
    string ArtistId,
    string AlbumId,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason,
    DateTimeOffset DeferredAtUtc) : RavenEventBodyDto;
