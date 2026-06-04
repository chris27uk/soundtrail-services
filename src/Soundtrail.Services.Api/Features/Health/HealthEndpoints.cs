using Microsoft.AspNetCore.Mvc;
using Soundtrail.Services.Features.Search.TrackSearch;

namespace Soundtrail.Services.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

        endpoints.MapGet(
            "/health/ready",
            async (
                [FromServices] ITrackSearchPort trackSearch,
                CancellationToken cancellationToken) =>
            {
                var isReady = await trackSearch.IsReadyAsync(cancellationToken);

                return isReady
                    ? Results.Ok(new { status = "ready" })
                    : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            });

        return endpoints;
    }
}
