namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record SongMetadataDto(
    string Title,
    string Artist,
    string? Isrc,
    string? Mbid,
    int? DurationMs,
    string? AlbumTitle = null,
    DateOnly? ReleaseDate = null,
    string? SourceArtistId = null,
    string? SourceAlbumId = null);
