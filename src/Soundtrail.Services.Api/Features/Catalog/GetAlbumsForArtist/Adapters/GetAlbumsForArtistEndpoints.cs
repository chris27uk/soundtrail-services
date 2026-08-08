using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;

public static class GetAlbumsForArtistEndpoints
{
    public static IEndpointRouteBuilder MapGetAlbumsForArtistEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/catalog/artists/{artistId}/albums",
            async (
                string artistId,
                IApiHandler<GetAlbumsForArtistRequest, GetAlbumsForArtistResponse?> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var request = new GetAlbumsForArtistRequest(ArtistId.From(artistId));
                var response = await handler.Handle(request, cancellationToken);
                var dto = response is null ? null : typeRegistry.ToDto<GetAlbumsForArtistResponseDto>(response);
                DiscoveryResponseHeaders.Apply(httpContext, dto?.Discovery);
                return response is null ? Results.NotFound() : Results.Ok(dto);
            });

        return endpoints;
    }
}
