using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;

namespace Soundtrail.Services.Api.Features.GetAlbum.Adapters;

public static class GetAlbumEndpoints
{
    public static IEndpointRouteBuilder MapGetAlbumEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}",
            async (string artistId, string albumId, IApiHandler<GetAlbumRequest, GetAlbumResponse?> handler, CancellationToken cancellationToken) =>
            {
                var objAlbumId = AlbumId.From(artistId, albumId);
                var response = await handler.Handle(new GetAlbumRequest(objAlbumId), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(typeRegistry.ToDto(response));
            });

        return endpoints;
    }
}
