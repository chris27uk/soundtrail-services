using Microsoft.Extensions.Diagnostics.HealthChecks;
using Soundtrail.Services.Api.Features.Search.TrackSearch;

namespace Soundtrail.Services.Api.Features.Health;

public sealed class TrackSearchReadyHealthCheck(ITrackSearchPort trackSearch) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isReady = await trackSearch.IsReadyAsync(cancellationToken);
        return isReady
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Track search is not ready.");
    }
}
