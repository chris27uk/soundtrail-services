using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;

public sealed class RavenGetAlbumsForArtistPort(IDocumentStore documentStore, ITypeRegistry typeRegistry) : IGetAlbumsForArtistPort
{
    public async Task<GetAlbumsForArtistResponse?> GetAlbumsForArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        var activeSession = documentStore.OpenAsyncSession();
        var documentId = CatalogArtistAlbumsRecordDto.GetDocumentId(artistId.Value);
        var existing = await activeSession.LoadAsync<CatalogArtistAlbumsRecordDto>(documentId, cancellationToken);
        return existing is null ? null : typeRegistry.ToDomainObject<GetAlbumsForArtistResponse>(existing);
    }
}
