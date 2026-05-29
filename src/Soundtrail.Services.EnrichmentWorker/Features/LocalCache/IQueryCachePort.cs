using Soundtrail.Services.EnrichmentWorker.Models;

namespace Soundtrail.Services.EnrichmentWorker.Ports;

public interface IQueryCachePort
{
    Task RefreshAsync(
        ResolutionDemand demand,
        TrackMapping mapping,
        CancellationToken cancellationToken);
}
