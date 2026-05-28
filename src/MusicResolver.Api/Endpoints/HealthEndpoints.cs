using MusicResolver.Application.Ports;

namespace MusicResolver.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

        endpoints.MapGet(
            "/health/ready",
            async (
                IQueryCachePort queryCache,
                ITrackLookupPort trackLookup,
                ITrackSearchPort trackSearch,
                CancellationToken cancellationToken) =>
            {
                var isReady =
                    await queryCache.IsReadyAsync(cancellationToken) &&
                    await trackLookup.IsReadyAsync(cancellationToken) &&
                    await trackSearch.IsReadyAsync(cancellationToken);

                return isReady
                    ? Results.Ok(new { status = "ready" })
                    : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            });

        return endpoints;
    }
}
