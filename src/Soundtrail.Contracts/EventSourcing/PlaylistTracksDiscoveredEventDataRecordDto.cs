namespace Soundtrail.Contracts.EventSourcing;

public sealed record PlaylistTracksDiscoveredEventDataRecordDto(
    string PlaylistId,
    string[] TrackIds,
    DateTimeOffset ObservedAtUtc) : RavenEventBodyDto;
