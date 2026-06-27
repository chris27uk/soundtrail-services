using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Services.Api.Features.GetArtist.Adapters;

public static class GetArtistEndpoints
{
    public static IEndpointRouteBuilder MapGetArtistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}",
            async (string artistId, IApiHandler<GetArtistCommand, ArtistDetailsResponse?> handler, CancellationToken cancellationToken) =>
            {
                var artist = ArtistId.From(artistId);
                var response = await handler.Handle(new GetArtistCommand(artist), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(TypeTranslationRegistry.Default.Translate<Soundtrail.Contracts.Api.ArtistDetailsResponseDto>(response));
            });

        return endpoints;
    }
}
