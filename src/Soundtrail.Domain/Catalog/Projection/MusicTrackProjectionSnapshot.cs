namespace Soundtrail.Domain.Model;

public sealed record MusicTrackProjectionSnapshot(
    string? ArtistId,
    string? AlbumId,
    string Title,
    ArtistName Artist,
    AlbumTitle AlbumTitle,
    string SearchText,
    string? Isrc,
    string NormalizedIsrc,
    string? Mbid,
    string NormalizedMbid,
    string? AppleId,
    string? SpotifyId,
    int? DurationMs,
    DateOnly? ReleaseDate,
    string? ArtworkUrl,
    ProjectedSongMetadata? ResolvedMetadata,
    ProjectedProviderReference? AppleReference,
    ProjectedProviderReference? YouTubeMusicReference,
    bool IsPlayable,
    int ProjectionVersion);
