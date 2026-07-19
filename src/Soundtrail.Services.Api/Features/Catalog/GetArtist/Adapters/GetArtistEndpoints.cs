using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;

public static class GetArtistEndpoints
{
    public static IEndpointRouteBuilder MapGetArtistEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/catalog/artists/{artistId}",
            async (string artistId, IApiHandler<GetArtistRequest, GetArtistResponse?> handler, CancellationToken cancellationToken) =>
            {
                var objArtistId = ArtistId.From(artistId);
                var response = await handler.Handle(new GetArtistRequest(objArtistId), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(typeRegistry.ToDto(response));
            });

        return endpoints;
    }
}
