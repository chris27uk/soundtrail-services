namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownTrackDiscoveryFailedEventDataRecordDto(
    string TrackId,
    string Priority,
    string Reason,
    DateTimeOffset FailedAtUtc) : RavenEventBodyDto;
