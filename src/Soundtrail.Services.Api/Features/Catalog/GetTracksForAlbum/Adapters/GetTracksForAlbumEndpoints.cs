using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;

public static class GetTracksForAlbumEndpoints
{
    public static IEndpointRouteBuilder MapGetTracksForAlbumEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/catalog/artists/{artistId}/albums/{albumId}/tracks",
            async (
                string artistId,
                string albumId,
                IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var resolvedAlbumId = AlbumId.From(artistId, albumId);
                var request = new GetTracksForAlbumRequest(resolvedAlbumId);
                var response = await handler.Handle(request, cancellationToken);
                var dto = response is null ? null : typeRegistry.ToDto<GetTracksForAlbumResponseDto>(response);
                DiscoveryResponseHeaders.Apply(httpContext, dto?.Discovery);
                return response is null ? Results.NotFound() : Results.Ok(dto);
            });

        return endpoints;
    }
}
