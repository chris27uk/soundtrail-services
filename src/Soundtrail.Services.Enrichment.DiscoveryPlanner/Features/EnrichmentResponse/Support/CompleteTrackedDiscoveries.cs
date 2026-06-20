using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Support;

public sealed class CompleteTrackedDiscoveries(
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICompleteTrackedDiscoveriesRepository discoveryRepository)
{
    public async Task CompleteAsync(
        Soundtrail.Domain.Responses.EnrichmentResponse response,
        CancellationToken cancellationToken)
    {
        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(response.MusicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, tracking.Criteria, cancellationToken);
            if (!discovery.Complete(response.Priority, "Discovery completed", response.CreatedAt))
            {
                continue;
            }

            await discovery.SaveAsync(discoveryRepository, cancellationToken);
        }
    }
}
