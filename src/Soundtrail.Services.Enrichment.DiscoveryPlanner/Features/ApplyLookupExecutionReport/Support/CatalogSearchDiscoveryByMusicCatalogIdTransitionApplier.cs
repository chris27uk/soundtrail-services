using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ApplyLookupExecutionReport.Support;

public sealed class CatalogSearchDiscoveryByMusicCatalogIdTransitionApplier(
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICatalogSearchDiscoveryRepository discoveryRepository)
{
    public async Task ApplyAsync(
        MusicCatalogId musicCatalogId,
        Func<CatalogSearchDiscovery, bool> transition,
        CancellationToken cancellationToken)
    {
        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, tracking.Criteria, cancellationToken);
            if (!transition(discovery))
            {
                continue;
            }

            await discovery.SaveAsync(discoveryRepository, cancellationToken);
        }
    }
}
