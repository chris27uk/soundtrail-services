using Microsoft.AspNetCore.Http.HttpResults;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;

public static class GetTracksForPlaylistEndpoints
{
    public static IEndpointRouteBuilder MapGetTracksForPlaylistEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/catalog/playlists/{playlistId}/tracks",
            async Task<Microsoft.AspNetCore.Http.HttpResults.Results<NotFound<GetTracksForPlaylistResponseDto>, Ok<GetTracksForPlaylistResponseDto>>> (
                string playlistId,
                IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?> handler,
                CancellationToken cancellationToken) =>
            {
                var request = new GetTracksForPlaylistRequest(PlaylistId.FromPlaylistName(playlistId));
                var response = await handler.Handle(request, cancellationToken);
                return response is null
                    ? TypedResults.NotFound(new GetTracksForPlaylistResponseDto(
                        request.PlaylistId.Value,
                        [],
                        null))
                    : TypedResults.Ok(typeRegistry.ToDto<GetTracksForPlaylistResponseDto>(response));
            });

        return endpoints;
    }
}
