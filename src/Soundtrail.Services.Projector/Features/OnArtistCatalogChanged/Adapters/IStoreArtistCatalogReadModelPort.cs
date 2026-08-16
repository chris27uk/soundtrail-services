using Soundtrail.Adapters.CatalogProjection;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;

public interface IStoreArtistCatalogReadModelPort
{
    Task StoreAsync(ArtistCatalogProjection projection, CancellationToken cancellationToken);
}
