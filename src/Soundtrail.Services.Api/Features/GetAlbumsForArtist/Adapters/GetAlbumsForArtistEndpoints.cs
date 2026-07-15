using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetAlbumsForArtist.Adapters;

public static class GetAlbumsForArtistEndpoints
{
    public static IEndpointRouteBuilder MapGetAlbumsForArtistEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums",
            async (
                string artistId,
                IApiHandler<GetAlbumsForArtistRequest, GetAlbumsForArtistResponse?> handler,
                CancellationToken cancellationToken) =>
            {
                var request = new GetAlbumsForArtistRequest(ArtistId.From(artistId));
                var response = await handler.Handle(request, cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(typeRegistry.ToDto(response));
            });

        return endpoints;
    }
}
