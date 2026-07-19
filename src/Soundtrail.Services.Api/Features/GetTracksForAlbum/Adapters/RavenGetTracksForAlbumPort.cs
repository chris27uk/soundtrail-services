using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForAlbum.Adapters;

public sealed class RavenGetTracksForAlbumPort(IDocumentStore documentStore, ITypeRegistry typeRegistry) : IGetTracksForAlbumPort
{
    public async Task<GetTracksForAlbumResponse?> GetTracksForAlbumAsync(AlbumId albumId, CancellationToken cancellationToken)
    {
        var activeSession = documentStore.OpenAsyncSession();
        var documentId = CatalogAlbumTracksRecordDto.GetDocumentId(albumId.StableValue);
        var existing = await activeSession.LoadAsync<CatalogAlbumTracksRecordDto>(documentId, cancellationToken);
        return existing is null ? null : typeRegistry.ToDomainObject<GetTracksForAlbumResponse>(existing);
    }
}
