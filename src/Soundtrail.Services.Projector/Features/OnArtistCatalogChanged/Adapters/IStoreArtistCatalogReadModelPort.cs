using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Projection;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;

public interface IStoreArtistCatalogReadModelPort
{
    Task StoreAsync(ArtistCatalogProjection projection, CancellationToken cancellationToken);
}
