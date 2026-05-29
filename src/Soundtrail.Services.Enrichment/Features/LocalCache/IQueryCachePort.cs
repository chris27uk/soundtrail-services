using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Ports;

public interface IQueryCachePort
{
    Task RefreshAsync(
        ResolutionDemand demand,
        TrackMapping mapping,
        CancellationToken cancellationToken);
}
