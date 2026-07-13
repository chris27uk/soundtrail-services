using Raven.Client.Documents;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetAlbumsForArtist.Adapters;

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
