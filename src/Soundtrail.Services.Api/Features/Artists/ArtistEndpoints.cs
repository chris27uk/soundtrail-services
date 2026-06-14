using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Services.Api.Features;

namespace Soundtrail.Services.Api.Features.Artists;

public static class ArtistEndpoints
{
    public static IEndpointRouteBuilder MapArtistEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}",
            async (string artistId, IHandler<GetArtistCommand, ArtistDetailsResponse?> handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.Handle(new GetArtistCommand(ArtistId.From(artistId)), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(ToContract(response));
            });

        endpoints.MapGet(
            "/artists/{artistId}/tracks",
            async (string artistId, IHandler<ListTracksByArtistCommand, ArtistTracksResponse?> handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.Handle(new ListTracksByArtistCommand(ArtistId.From(artistId)), cancellationToken);
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

    private static object ToContract(ArtistTracksResponse response) => new
    {
        artistId = response.ArtistId.Value,
        artistName = response.ArtistName,
        tracks = response.Tracks.Select(ToContract)
    };

    private static object ToContract(AlbumSummary album) => new
    {
        id = album.AlbumId.Value,
        name = album.Name,
        releaseDate = album.ReleaseDate,
        playabilityStatus = album.PlayabilityStatus.ToString(),
        availableProviders = album.AvailableProviders.Select(ProviderContract.ToValue),
        terminallyUnavailableProviders = album.TerminallyUnavailableProviders.Select(ProviderContract.ToValue)
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
