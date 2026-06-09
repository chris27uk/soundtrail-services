namespace Soundtrail.Contracts.Responses;

public sealed record SongMetadataDto(
    string Title,
    string Artist,
    string? Isrc,
    string? Mbid,
    int? DurationMs);
