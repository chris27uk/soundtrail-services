namespace Soundtrail.Services.Api.Features.GetAlbum.Registrations
{
    public record GetAlbumResponseDto(
        string ArtistId,
        string ArtistName,
        string AlbumId,
        DateOnly? ReleaseDate);
}
