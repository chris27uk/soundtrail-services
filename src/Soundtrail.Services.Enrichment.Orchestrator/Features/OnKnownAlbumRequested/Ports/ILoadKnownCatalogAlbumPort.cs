using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Ports;

public interface ILoadKnownCatalogAlbumPort
{
    Task<KnownCatalogAlbumLookupData?> LoadAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken);
}
