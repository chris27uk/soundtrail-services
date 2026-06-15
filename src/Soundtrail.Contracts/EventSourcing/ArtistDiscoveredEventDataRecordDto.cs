namespace Soundtrail.Contracts.EventSourcing;

public sealed record ArtistDiscoveredEventDataRecordDto(
    string? ArtistId,
    string? ArtistName,
    string SourceProvider,
    DateTimeOffset ObservedAt);
