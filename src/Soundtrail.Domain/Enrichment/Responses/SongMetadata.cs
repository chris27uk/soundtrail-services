namespace Soundtrail.Domain.Responses;

public sealed record SongMetadata(
    string Title,
    string Artist,
    string? Isrc,
    string? Mbid,
    int? DurationMs,
    string? AlbumTitle = null,
    DateOnly? ReleaseDate = null,
    string? SourceArtistId = null,
    string? SourceAlbumId = null);
