using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;

public interface IStoreArtistCatalogReadModelPort
{
    Task StoreAsync(ArtistCatalogReadModel readModel, CancellationToken cancellationToken);
}
