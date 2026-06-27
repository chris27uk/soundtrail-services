namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownTrackRequestedEventDataRecordDto(
    string TrackId,
    string Playback,
    DateTimeOffset RequestedAtUtc,
    string CorrelationId);
