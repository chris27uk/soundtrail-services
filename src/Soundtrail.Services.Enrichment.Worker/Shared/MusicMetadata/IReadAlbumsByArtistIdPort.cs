using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

public interface IReadAlbumsByArtistIdPort
{
    Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(
        ArtistId artistId,
        CancellationToken cancellationToken);
}
