using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Services.Api.Features.ListTracksByArtist.Adapters;

public static class ListTracksByArtistEndpoints
{
    public static IEndpointRouteBuilder MapListTracksByArtistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}/tracks",
            async (string artistId, IApiHandler<ListTracksByArtistCommand, ArtistTracksResponse?> handler, CancellationToken cancellationToken) =>
            {
                var artist = ArtistId.From(artistId);
                var response = await handler.Handle(new ListTracksByArtistCommand(artist), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(TypeTranslationRegistry.Default.ToDto<Soundtrail.Contracts.Api.ArtistTracksResponseDto>(response));
            });

        return endpoints;
    }
}
