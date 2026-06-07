namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public interface IProviderSnapshotStore
{
    Task SaveAsync(
        ProviderSnapshot snapshot,
        CancellationToken cancellationToken);
}
