using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;

public sealed class RavenGetArtistPort(IDocumentStore documentStore, ITypeRegistry typeRegistry) : IGetArtistPort
{
    public async Task<GetArtistResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        var activeSession = documentStore.OpenAsyncSession();
        var documentId = CatalogArtistRecordDto.GetDocumentId(artistId.Value);
        var existing = await activeSession.LoadAsync<CatalogArtistRecordDto>(documentId, cancellationToken);
        return existing is null ? null : typeRegistry.ToDomainObject<GetArtistResponse>(existing);
    }
}
