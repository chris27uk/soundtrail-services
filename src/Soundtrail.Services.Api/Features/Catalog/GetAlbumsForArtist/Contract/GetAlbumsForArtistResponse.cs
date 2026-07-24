using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

public sealed record GetAlbumsForArtistResponse(
    ArtistId ArtistId,
    ArtistName ArtistName,
    GetAlbumsForArtistAlbumResponse[] Albums,
    DiscoveryFeedbackResponse? Discovery = null);

public sealed record GetAlbumsForArtistAlbumResponse(
    AlbumId AlbumId,
    CatalogItemId MusicCatalogId,
    string AlbumTitle,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
