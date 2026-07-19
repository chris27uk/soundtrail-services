using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;

public static class GetTracksForArtistEndpoints
{
    public static IEndpointRouteBuilder MapGetTracksForArtistEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/catalog/artists/{artistId}/tracks",
            async (
                string artistId,
                IApiHandler<GetTracksForArtistRequest, GetTracksForArtistResponse?> handler,
                CancellationToken cancellationToken) =>
            {
                var request = new GetTracksForArtistRequest(ArtistId.From(artistId));
                var response = await handler.Handle(request, cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(typeRegistry.ToDto(response));
            });

        return endpoints;
    }
}
