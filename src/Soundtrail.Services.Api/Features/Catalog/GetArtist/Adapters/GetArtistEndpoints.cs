using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;
using Soundtrail.Services.Api.Shared.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;

public static class GetArtistEndpoints
{
    public static IEndpointRouteBuilder MapGetArtistEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/catalog/artists/{artistId}",
            async (string artistId, IApiHandler<GetArtistRequest, GetArtistResponse?> handler, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var objArtistId = ArtistId.From(artistId);
                var response = await handler.Handle(new GetArtistRequest(objArtistId), cancellationToken);
                var dto = response is null ? null : typeRegistry.ToDto<GetArtistResponseDto>(response);
                DiscoveryResponseHeaders.Apply(httpContext, dto?.Discovery);
                return response is null ? Results.NotFound() : Results.Ok(dto);
            });

        return endpoints;
    }
}
