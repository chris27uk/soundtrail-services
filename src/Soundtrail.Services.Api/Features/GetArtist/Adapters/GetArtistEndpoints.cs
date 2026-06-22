using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.GetArtist.Adapters;

public static class GetArtistEndpoints
{
    public static IEndpointRouteBuilder MapGetArtistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}",
            async (string artistId, IHandler<GetArtistCommand, ArtistDetailsResponse?> handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.Handle(new GetArtistCommand(ArtistId.From(artistId)), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(ToContract(response));
            });

        return endpoints;
    }

    private static object ToContract(ArtistDetailsResponse response) => new
    {
        id = response.ArtistId.Value,
        name = response.Name,
        albums = response.Albums.Select(ToContract)
    };

    private static object ToContract(AlbumSummary album) => new
    {
        id = album.AlbumId.Value,
        name = album.Name,
        releaseDate = album.ReleaseDate,
        playabilityStatus = album.PlayabilityStatus.ToString(),
        availableProviders = album.AvailableProviders.Select(providerName => providerName.ToPersistentId()),
        terminallyUnavailableProviders = album.TerminallyUnavailableProviders.Select(providerName => providerName.ToPersistentId())
    };
}
