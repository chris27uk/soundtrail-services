using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;

public sealed record GetAlbumsForArtistResponseDto(
    string ArtistId,
    string ArtistName,
    GetAlbumsForArtistAlbumResponseDto[] Albums,
    DiscoveryFeedbackResponseDto? Discovery);

public sealed record GetAlbumsForArtistAlbumResponseDto(
    string AlbumId,
    string MusicCatalogId,
    string AlbumTitle,
    DateOnly? ReleaseDate,
    string? ArtworkUrl);
