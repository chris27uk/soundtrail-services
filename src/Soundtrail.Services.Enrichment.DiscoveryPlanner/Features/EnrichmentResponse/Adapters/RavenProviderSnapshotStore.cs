using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters.Documents;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

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
