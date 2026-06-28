namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownTrackDiscoveryCompletedEventDataRecordDto(
    string TrackId,
    string Priority,
    string Reason,
    DateTimeOffset CompletedAtUtc) : RavenEventBodyDto;
