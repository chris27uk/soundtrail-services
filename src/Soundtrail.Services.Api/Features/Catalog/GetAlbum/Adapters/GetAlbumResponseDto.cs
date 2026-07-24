using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters
{
    public record GetAlbumResponseDto(
        string ArtistId,
        string ArtistName,
        string AlbumId,
        DateOnly? ReleaseDate,
        DiscoveryFeedbackResponseDto? Discovery);
}
