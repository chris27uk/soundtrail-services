namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

public interface IProviderSnapshotStore
{
    Task SaveAsync(
        ProviderSnapshot snapshot,
        CancellationToken cancellationToken);
}
