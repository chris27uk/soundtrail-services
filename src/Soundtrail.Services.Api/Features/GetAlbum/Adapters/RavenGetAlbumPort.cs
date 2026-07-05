using Raven.Client.Documents;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;

namespace Soundtrail.Services.Api.Features.GetAlbum.Adapters
{
    public class RavenGetAlbumPort(IDocumentStore documentStore, ITypeRegistry typeRegistry) : IGetAlbumPort
    {
        public async Task<GetAlbumResponse?> GetAlbumAsync(AlbumId albumId, CancellationToken cancellationToken)
        {
            var activeSession = documentStore.OpenAsyncSession();
            var documentId = CatalogAlbumRecordDto.GetDocumentId(albumId.ArtistAlbumId);
            var existing = await activeSession.LoadAsync<CatalogAlbumRecordDto>(documentId, cancellationToken);
           return existing is null ? null : typeRegistry.ToDomainObject<GetAlbumResponse>(existing);
        }
    }
}
