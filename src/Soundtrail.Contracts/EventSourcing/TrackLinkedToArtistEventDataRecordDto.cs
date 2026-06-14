namespace Soundtrail.Contracts.EventSourcing;

public sealed record TrackLinkedToArtistEventDataRecordDto(
    string? ArtistId,
    string? ArtistName,
    string SourceProvider,
    DateTimeOffset ObservedAt);
