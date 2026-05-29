using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.LocalCache;

public interface IQueryCachePort
{
    Task RefreshAsync(
        ResolutionDemand demand,
        TrackMapping mapping,
        CancellationToken cancellationToken);
}
