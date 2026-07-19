namespace Soundtrail.Services.Api.Features.GetAlbum.Adapters
{
    public record GetAlbumResponseDto(
        string ArtistId,
        string ArtistName,
        string AlbumId,
        DateOnly? ReleaseDate);
}
