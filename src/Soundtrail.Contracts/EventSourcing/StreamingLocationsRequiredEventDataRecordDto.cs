using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.EventSourcing;

public sealed record StreamingLocationsRequiredEventDataRecordDto(
    string MusicCatalogId,
    string Priority,
    string CorrelationId,
    string SourceProvider,
    DateTimeOffset ObservedAt,
    MusicSearchKind SearchKind,
    string? Query,
    string? Isrc,
    string? Title,
    string? Artist,
    string? Album,
    string? ArtistId,
    string? AlbumId) : RavenEventBodyDto;
