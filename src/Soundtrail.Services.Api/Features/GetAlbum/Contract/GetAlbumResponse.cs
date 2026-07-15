using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Api.Features.GetAlbum.Contract
{
    public sealed record GetAlbumResponse(
        ArtistId ArtistId,
        ArtistName ArtistName,
        AlbumId AlbumId,
        string AlbumName,
        DateOnly? ReleaseDate);
}
