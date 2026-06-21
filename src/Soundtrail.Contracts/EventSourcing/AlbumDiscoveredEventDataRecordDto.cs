namespace Soundtrail.Contracts.EventSourcing;

public sealed record AlbumDiscoveredEventDataRecordDto(
    string? AlbumId,
    string? AlbumTitle,
    DateOnly? ReleaseDate,
    string SourceProvider,
    DateTimeOffset ObservedAt);
