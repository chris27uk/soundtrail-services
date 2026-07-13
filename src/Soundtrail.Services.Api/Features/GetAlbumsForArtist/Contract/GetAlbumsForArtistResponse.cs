using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;

public sealed record GetAlbumsForArtistResponse(
    ArtistId ArtistId,
    ArtistName ArtistName,
    GetAlbumsForArtistAlbumResponse[] Albums);

public sealed record GetAlbumsForArtistAlbumResponse(
    AlbumId AlbumId,
    MusicCatalogId MusicCatalogId,
    string AlbumTitle,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
