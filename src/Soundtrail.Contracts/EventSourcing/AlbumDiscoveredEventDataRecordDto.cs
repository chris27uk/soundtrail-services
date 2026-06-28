namespace Soundtrail.Contracts.EventSourcing;

public sealed record AlbumDiscoveredEventDataRecordDto(
    string? AlbumId,
    string? AlbumTitle,
    string? SourceAlbumId,
    DateOnly? ReleaseDate,
    string SourceProvider,
    DateTimeOffset ObservedAt) : RavenEventBodyDto;
