namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;

public sealed record GetTracksForAlbumResponseDto(
    string ArtistId,
    string AlbumId,
    string AlbumTitle,
    GetTracksForAlbumTrackResponseDto[] Tracks);

public sealed record GetTracksForAlbumTrackResponseDto(
    string TrackId,
    string MusicCatalogId,
    string Title,
    string ArtistName,
    int? DurationMs,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
