namespace Soundtrail.Services.Api.Features.GetTracksForPlaylist.Adapters;

public sealed record GetTracksForPlaylistResponseDto(
    string PlaylistId,
    GetTracksForPlaylistTrackResponseDto[] Tracks);

public sealed record GetTracksForPlaylistTrackResponseDto(
    string TrackId,
    string MusicCatalogId,
    string Title,
    string ArtistName,
    string? AlbumTitle,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
