namespace Soundtrail.Contracts.EventSourcing;

public sealed record TrackDiscoveredEventDataRecordDto(
    string? MusicCatalogId,
    string Title,
    string Artist,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    string SourceProvider,
    DateTimeOffset ObservedAt) : RavenEventBodyDto;
