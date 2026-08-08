using Microsoft.AspNetCore.Http.HttpResults;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Shared.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;

public static class GetTracksForPlaylistEndpoints
{
    public static IEndpointRouteBuilder MapGetTracksForPlaylistEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/catalog/playlists/{playlistId}/tracks",
            async Task<Results<NotFound<GetTracksForPlaylistResponseDto>, Ok<GetTracksForPlaylistResponseDto>>> (
                string playlistId,
                IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var request = new GetTracksForPlaylistRequest(PlaylistId.FromPlaylistName(playlistId));
                var response = await handler.Handle(request, cancellationToken);
                var dto = response is null
                    ? new GetTracksForPlaylistResponseDto(
                        request.PlaylistId.Value,
                        [],
                        null)
                    : typeRegistry.ToDto<GetTracksForPlaylistResponseDto>(response);
                DiscoveryResponseHeaders.Apply(httpContext, dto.Discovery);

                return response is null
                    ? TypedResults.NotFound(dto)
                    : TypedResults.Ok(dto);
            });

        return endpoints;
    }
}
