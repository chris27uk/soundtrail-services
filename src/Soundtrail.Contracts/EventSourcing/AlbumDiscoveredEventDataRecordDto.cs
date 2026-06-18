namespace Soundtrail.Contracts.EventSourcing;

public sealed record AlbumDiscoveredEventDataRecordDto(
    string? AlbumId,
    string? AlbumTitle,
    string SourceProvider,
    DateTimeOffset ObservedAt);
