namespace Soundtrail.Contracts.EventSourcing;

public sealed record TrackDiscoveredEventDataRecordDto(
    string? MusicCatalogId,
    string? ArtistId,
    string? AlbumId,
    string? TrackIdBaseKeyHigh,
    string? TrackIdBaseKeyLow,
    string? TrackIdSpecificKey,
    string Title,
    string Artist,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    DateOnly? ReleaseDate,
    string? ReleaseType,
    string SourceProvider,
    DateTimeOffset ObservedAt) : RavenEventBodyDto;
