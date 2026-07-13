using Raven.Client.Documents;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetTracksForArtist.Adapters;

public sealed class RavenGetTracksForArtistPort(IDocumentStore documentStore, ITypeRegistry typeRegistry) : IGetTracksForArtistPort
{
    public async Task<GetTracksForArtistResponse?> GetTracksForArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        var activeSession = documentStore.OpenAsyncSession();
        var documentId = CatalogArtistTracksRecordDto.GetDocumentId(artistId.Value);
        var existing = await activeSession.LoadAsync<CatalogArtistTracksRecordDto>(documentId, cancellationToken);
        return existing is null ? null : typeRegistry.ToDomainObject<GetTracksForArtistResponse>(existing);
    }
}
