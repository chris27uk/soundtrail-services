using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Services.Api.Features.GetAlbum.Adapters;

public static class GetAlbumEndpoints
{
    public static IEndpointRouteBuilder MapGetAlbumEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}",
            async (string artistId, string albumId, IApiHandler<GetAlbumCommand, AlbumDetailsResponse?> handler, CancellationToken cancellationToken) =>
            {
                var artist = ArtistId.From(artistId);
                var album = AlbumId.From(albumId);
                var response = await handler.Handle(new GetAlbumCommand(artist, album), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(TypeTranslationRegistry.Default.ToDto<Soundtrail.Contracts.Api.AlbumDetailsResponseDto>(response));
            });

        return endpoints;
    }
}
