using Raven.Client.Documents;
using Soundtrail.Adapters.CatalogProjection;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;

public sealed class RavenStoreArtistCatalogReadModelPort(IDocumentStore documentStore) : IStoreArtistCatalogReadModelPort
{
    public async Task StoreAsync(ArtistCatalogProjection projection, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        foreach (var (id, document) in ArtistCatalogProjectionDocuments.CreateBrowseDocuments(projection))
        {
            await session.StoreAsync(document, id, cancellationToken);
        }

        session.Advanced.WaitForIndexesAfterSaveChanges(
            timeout: TimeSpan.FromSeconds(30),
            throwOnTimeout: false);
        await session.SaveChangesAsync(cancellationToken);
    }
}
