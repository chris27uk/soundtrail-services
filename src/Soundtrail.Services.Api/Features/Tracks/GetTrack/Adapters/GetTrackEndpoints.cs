using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Services.Api.Shared;

namespace Soundtrail.Services.Api.Features.Tracks.GetTrack.Adapters;

public static class GetTrackEndpoints
{
    public static IEndpointRouteBuilder MapGetTrackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/artists/{artistId}/albums/{albumId}/tracks/{trackId}",
            async (string artistId, string albumId, string trackId, IHandler<GetTrackCommand, TrackDetailsResponse?> handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.Handle(
                    new GetTrackCommand(ArtistId.From(artistId), AlbumId.From(albumId), TrackId.From(trackId)),
                    cancellationToken);

                return response is null ? Results.NotFound() : Results.Ok(ToContract(response));
            });

        return endpoints;
    }

    private static object ToContract(TrackDetailsResponse response) => new
    {
        artistId = response.ArtistId.Value,
        artistName = response.ArtistName,
        albumId = response.AlbumId.Value,
        albumName = response.AlbumName,
        id = response.TrackId.Value,
        title = response.Title,
        isrc = response.Isrc,
        durationMs = response.DurationMs,
        playabilityStatus = response.PlayabilityStatus.ToString(),
        availableProviders = response.AvailableProviders.Select(ProviderContract.ToValue),
        terminallyUnavailableProviders = response.TerminallyUnavailableProviders.Select(ProviderContract.ToValue)
    };
}
