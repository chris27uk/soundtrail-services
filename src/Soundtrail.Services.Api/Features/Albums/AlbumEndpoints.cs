using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Services.Api.Features;

namespace Soundtrail.Services.Api.Features.Albums;

public static class AlbumEndpoints
{
    public static IEndpointRouteBuilder MapAlbumEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}",
            async (string artistId, string albumId, IHandler<GetAlbumCommand, AlbumDetailsResponse?> handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.Handle(new GetAlbumCommand(ArtistId.From(artistId), AlbumId.From(albumId)), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(ToContract(response));
            });

        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}/tracks",
            async (string artistId, string albumId, IHandler<ListTracksByAlbumCommand, AlbumTracksResponse?> handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.Handle(new ListTracksByAlbumCommand(ArtistId.From(artistId), AlbumId.From(albumId)), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(ToContract(response));
            });

        return endpoints;
    }

    private static object ToContract(AlbumDetailsResponse response) => new
    {
        artistId = response.ArtistId.Value,
        artistName = response.ArtistName,
        id = response.AlbumId.Value,
        name = response.Name,
        releaseDate = response.ReleaseDate,
        tracks = response.Tracks.Select(ToContract)
    };

    private static object ToContract(AlbumTracksResponse response) => new
    {
        artistId = response.ArtistId.Value,
        artistName = response.ArtistName,
        albumId = response.AlbumId.Value,
        albumName = response.AlbumName,
        tracks = response.Tracks.Select(ToContract)
    };

    private static object ToContract(TrackSummary track) => new
    {
        id = track.TrackId.Value,
        title = track.Title,
        albumId = track.AlbumId.Value,
        albumName = track.AlbumName,
        isrc = track.Isrc,
        durationMs = track.DurationMs,
        playabilityStatus = track.PlayabilityStatus.ToString(),
        availableProviders = track.AvailableProviders.Select(ProviderContract.ToValue),
        terminallyUnavailableProviders = track.TerminallyUnavailableProviders.Select(ProviderContract.ToValue)
    };
}
