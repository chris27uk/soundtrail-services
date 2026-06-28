namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownTrackDiscoveryStartedEventDataRecordDto(
    string TrackId,
    string Priority,
    string Reason,
    DateTimeOffset StartedAtUtc) : RavenEventBodyDto;
