using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Support;

public sealed class CaptureProviderSnapshot(IProviderSnapshotStore snapshotStore)
{
    public Task CaptureAsync(
        ProviderSnapshot snapshot,
        CancellationToken cancellationToken) =>
        snapshotStore.SaveAsync(snapshot, cancellationToken);
}
