using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.GetAlbumsForArtist;

internal static class ArtistAlbums
{
    public static ArtistId DefaultArtistId => ArtistId.From("artist-1701");

    public static GetAlbumsForArtistResponse CreateResponse(
        ArtistId? artistId = null,
        string artistName = "The Artist",
        string albumId = "album-1801",
        string albumTitle = "The Album",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/albums/album-1801.jpg")
    {
        var resolvedArtistId = artistId ?? DefaultArtistId;
        var resolvedAlbumId = AlbumId.From(resolvedArtistId.Value, albumId);

        return new GetAlbumsForArtistResponse(
            resolvedArtistId,
            ArtistName.From(artistName),
            [
                new GetAlbumsForArtistAlbumResponse(
                    resolvedAlbumId,
                    new MusicCatalogId.Album(resolvedAlbumId),
                    albumTitle,
                    releaseDate ?? new DateOnly(2024, 1, 2),
                    artworkUrl)
            ]);
    }
}
