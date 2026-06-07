using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven;

public sealed class RavenProviderSnapshotStore(
    IAsyncDocumentSession session) : IProviderSnapshotStore
{
    public Task SaveAsync(
        ProviderSnapshot snapshot,
        CancellationToken cancellationToken) =>
        session.StoreAsync(
            new RavenProviderSnapshotDocument
            {
                Id = RavenProviderSnapshotDocument.GetDocumentId(snapshot.MusicCatalogId.Value, snapshot.Provider.ToString()),
                MusicCatalogId = snapshot.MusicCatalogId.Value,
                Provider = snapshot.Provider.ToString(),
                CapturedAt = snapshot.CapturedAt,
                PayloadJson = snapshot.PayloadJson
            },
            cancellationToken);
}
