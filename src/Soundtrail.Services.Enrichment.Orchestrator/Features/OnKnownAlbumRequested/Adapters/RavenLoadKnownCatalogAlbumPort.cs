using Raven.Client.Documents;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Ports;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Adapters;

public sealed class RavenLoadKnownCatalogAlbumPort(IDocumentStore documentStore) : ILoadKnownCatalogAlbumPort
{
    public async Task<KnownCatalogAlbumLookupData?> LoadAsync(
        ArtistId artistId,
        AlbumId albumId,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var document = await session.LoadAsync<CatalogAlbumRecordDto>(
            CatalogAlbumRecordDto.GetDocumentId(albumId.ArtistAlbumId),
            cancellationToken);

        if (document is null)
        {
            return null;
        }

        if (!string.Equals(document.ArtistId, artistId.Value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Known album '{albumId.ArtistAlbumId}' is not owned by artist '{artistId.Value}'.");
        }

        var artist = await session.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(artistId.Value),
            cancellationToken);

        return new KnownCatalogAlbumLookupData(
            document.ArtistName,
            document.Name,
            artist?.MusicBrainzArtistId,
            document.MusicBrainzReleaseId);
    }
}
