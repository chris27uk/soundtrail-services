namespace Soundtrail.Services.Api.Features.GetAlbumsForArtist.Registrations;

public sealed record GetAlbumsForArtistResponseDto(
    string ArtistId,
    string ArtistName,
    GetAlbumsForArtistAlbumResponseDto[] Albums);

public sealed record GetAlbumsForArtistAlbumResponseDto(
    string AlbumId,
    string MusicCatalogId,
    string AlbumTitle,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
