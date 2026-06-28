using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Services.Api.Features.ListTracksByAlbum.Adapters;

public static class ListTracksByAlbumEndpoints
{
    public static IEndpointRouteBuilder MapListTracksByAlbumEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}/tracks",
            async (string artistId, string albumId, IApiHandler<ListTracksByAlbumCommand, AlbumTracksResponse?> handler, CancellationToken cancellationToken) =>
            {
                var artist = ArtistId.From(artistId);
                var album = AlbumId.From(albumId);
                var response = await handler.Handle(new ListTracksByAlbumCommand(artist, album), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(TypeTranslationRegistry.Default.Translate<Soundtrail.Contracts.Api.AlbumTracksResponseDto>(response));
            });

        return endpoints;
    }
}
