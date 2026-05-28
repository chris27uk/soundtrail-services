using Soundtrail.Services.Features.CatalogLookup;
using Soundtrail.Services.Features.CatalogLookup.Models;

namespace Soundtrail.Services.Api.Features.CatalogLookup;

public static class CatalogLookupEndpoints
{
    public static IEndpointRouteBuilder MapCatalogLookupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/lookup",
            async (
                string? isrc,
                string? appleId,
                string? mbid,
                string? spotifyId,
                int? durationMs,
                CatalogLookupHandler handler,
                CancellationToken cancellationToken) =>
            {
                CatalogLookupRequest request;

                try
                {
                    request = CatalogLookupRequest.Create(
                        isrc,
                        appleId,
                        mbid,
                        spotifyId,
                        durationMs);
                }
                catch (Exception ex) when (
                    ex is ArgumentException or ArgumentOutOfRangeException)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }

                var track = await handler.Handle(request, cancellationToken);

                return track is null
                    ? Results.NotFound()
                    : Results.Ok(new
                    {
                        track = new
                        {
                            title = track.Title.Value,
                            artist = track.Artist.Value,
                            isrc = track.Isrc?.Value,
                            mbid = track.Mbid?.Value,
                            appleId = track.AppleId?.Value,
                            spotifyId = track.SpotifyId?.Value,
                            durationMs = track.Duration?.Value
                        }
                    });
            });

        return endpoints;
    }
}
