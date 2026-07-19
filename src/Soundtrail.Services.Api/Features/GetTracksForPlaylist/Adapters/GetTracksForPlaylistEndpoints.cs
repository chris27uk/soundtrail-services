using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Services.Api.Features.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForPlaylist.Adapters;

public static class GetTracksForPlaylistEndpoints
{
    public static IEndpointRouteBuilder MapGetTracksForPlaylistEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/playlists/{playlistId}/tracks",
            async (
                string playlistId,
                IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?> handler,
                CancellationToken cancellationToken) =>
            {
                var request = new GetTracksForPlaylistRequest(PlaylistId.FromPlaylistName(playlistId));
                var response = await handler.Handle(request, cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(typeRegistry.ToDto(response));
            });

        return endpoints;
    }
}
