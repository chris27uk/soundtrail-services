using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;

public sealed class RavenStoreCatalogSearchCandidatePort(IDocumentStore documentStore) : IStoreCatalogSearchCandidatePort
{
    public async Task StoreAsync(CatalogSearchCandidateProjection projection, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(
            new CatalogSearchCandidateRecordDto
            {
                Id = CatalogSearchCandidateRecordDto.GetDocumentId(projection.CatalogItemId),
                CatalogItemId = projection.CatalogItemId,
                CandidateKind = projection.CandidateKind,
                SearchText = projection.SearchText,
                Title = projection.Title,
                ArtistName = projection.ArtistName,
                AlbumTitle = projection.AlbumTitle,
                ArtworkUrl = projection.ArtworkUrl,
                UpdatedAt = projection.UpdatedAt
            },
            cancellationToken);

        await session.SaveChangesAsync(cancellationToken);
    }
}
