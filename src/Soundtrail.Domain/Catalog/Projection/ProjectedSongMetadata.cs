namespace Soundtrail.Domain.Catalog.Projection;

public sealed record ProjectedSongMetadata(
    string Title,
    ArtistName Artist,
    string? Isrc,
    string NormalizedIsrc,
    string? Mbid,
    string NormalizedMbid,
    int? DurationMs);
