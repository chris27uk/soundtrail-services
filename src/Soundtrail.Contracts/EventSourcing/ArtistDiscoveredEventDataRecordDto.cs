namespace Soundtrail.Contracts.EventSourcing;

public sealed record ArtistDiscoveredEventDataRecordDto(
    string? ArtistId,
    string? ArtistName,
    string? SourceArtistId,
    string SourceProvider,
    DateTimeOffset ObservedAt);
