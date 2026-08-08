using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;
using Soundtrail.Services.Api.Shared.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;

public static class GetAlbumEndpoints
{
    public static IEndpointRouteBuilder MapGetAlbumEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/catalog/artists/{artistId}/albums/{albumId}",
            async (string artistId, string albumId, IApiHandler<GetAlbumRequest, GetAlbumResponse?> handler, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var objAlbumId = AlbumId.From(artistId, albumId);
                var response = await handler.Handle(new GetAlbumRequest(objAlbumId), cancellationToken);
                var dto = response is null ? null : typeRegistry.ToDto<GetAlbumResponseDto>(response);
                DiscoveryResponseHeaders.Apply(httpContext, dto?.Discovery);
                return response is null ? Results.NotFound() : Results.Ok(dto);
            });

        return endpoints;
    }
}
