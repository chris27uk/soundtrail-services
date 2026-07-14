using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.GetArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetArtist.Adapters;

public static class GetArtistEndpoints
{
    public static IEndpointRouteBuilder MapGetArtistEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/artists/{artistId}",
            async (string artistId, IApiHandler<GetArtistRequest, GetArtistResponse?> handler, CancellationToken cancellationToken) =>
            {
                var objArtistId = ArtistId.From(artistId);
                var response = await handler.Handle(new GetArtistRequest(objArtistId), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(typeRegistry.ToDto(response));
            });

        return endpoints;
    }
}
