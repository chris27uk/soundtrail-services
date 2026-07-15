using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForAlbum.Adapters;

public static class GetTracksForAlbumEndpoints
{
    public static IEndpointRouteBuilder MapGetTracksForAlbumEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}/tracks",
            async (
                string artistId,
                string albumId,
                IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?> handler,
                CancellationToken cancellationToken) =>
            {
                var resolvedAlbumId = AlbumId.From(artistId, albumId);
                var request = new GetTracksForAlbumRequest(resolvedAlbumId);
                var response = await handler.Handle(request, cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(typeRegistry.ToDto(response));
            });

        return endpoints;
    }
}
