using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.Search.Contracts;

namespace Soundtrail.Services.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

        endpoints.MapGet(
            "/health/ready",
            async (
                ICatalogLookupPort trackLookup,
                ITrackSearchPort trackSearch,
                CancellationToken cancellationToken) =>
            {
                var isReady =
                    await trackLookup.IsReadyAsync(cancellationToken) &&
                    await trackSearch.IsReadyAsync(cancellationToken);

                return isReady
                    ? Results.Ok(new { status = "ready" })
                    : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            });

        return endpoints;
    }
}
