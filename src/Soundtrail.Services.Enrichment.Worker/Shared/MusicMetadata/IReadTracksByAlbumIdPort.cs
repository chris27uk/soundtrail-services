using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

public interface IReadTracksByAlbumIdPort
{
    Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(
        AlbumId albumId,
        CancellationToken cancellationToken);
}
