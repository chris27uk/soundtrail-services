using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Support;

public sealed class TrackedDiscoveryStartMarker(
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICatalogSearchDiscoveryRepository discoveryRepository)
{
    public async Task MarkAsync(
        MusicCatalogId musicCatalogId,
        LookupPriorityBand priority,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(
                discoveryRepository,
                tracking.Criteria,
                cancellationToken);

            if (!discovery.Start(priority, "Lookup started", now))
            {
                continue;
            }

            await discovery.SaveAsync(discoveryRepository, cancellationToken);
        }
    }
}
