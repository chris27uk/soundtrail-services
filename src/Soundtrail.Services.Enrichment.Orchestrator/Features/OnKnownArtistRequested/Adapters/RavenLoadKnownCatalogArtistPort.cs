using Raven.Client.Documents;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Ports;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Adapters;

public sealed class RavenLoadKnownCatalogArtistPort(IDocumentStore documentStore) : ILoadKnownCatalogArtistPort
{
    public async Task<KnownCatalogArtistLookupData?> LoadAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var document = await session.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(artistId.Value),
            cancellationToken);

        return document is null
            ? null
            : new KnownCatalogArtistLookupData(
                document.Name,
                document.MusicBrainzArtistId);
    }
}
