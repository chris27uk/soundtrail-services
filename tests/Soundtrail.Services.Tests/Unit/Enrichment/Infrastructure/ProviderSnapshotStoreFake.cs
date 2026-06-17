using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class ProviderSnapshotStoreFake : IProviderSnapshotStore
{
    private readonly Dictionary<string, ProviderSnapshot> snapshots = [];

    public IReadOnlyDictionary<string, ProviderSnapshot> Snapshots => snapshots;

    public Task SaveAsync(
        ProviderSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        snapshots[$"{snapshot.MusicCatalogId.Value}:{snapshot.Provider}"] = snapshot;
        return Task.CompletedTask;
    }
}
