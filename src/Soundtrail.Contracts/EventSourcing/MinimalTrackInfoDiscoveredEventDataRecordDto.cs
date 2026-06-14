namespace Soundtrail.Contracts.EventSourcing;

public sealed record MinimalTrackInfoDiscoveredEventDataRecordDto(
    string Title,
    string Artist,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    string SourceProvider,
    DateTimeOffset ObservedAt);
