namespace Soundtrail.Contracts.EventSourcing;

public sealed record TrackLinkedToAlbumEventDataRecordDto(
    string? AlbumId,
    string? AlbumTitle,
    string SourceProvider,
    DateTimeOffset ObservedAt);
