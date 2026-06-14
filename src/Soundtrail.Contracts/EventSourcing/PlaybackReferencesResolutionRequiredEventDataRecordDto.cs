namespace Soundtrail.Contracts.EventSourcing;

public sealed record PlaybackReferencesResolutionRequiredEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    string CorrelationId,
    string SourceProvider,
    DateTimeOffset ObservedAt,
    string? Isrc,
    string? Title,
    string? Artist,
    string? Album);
