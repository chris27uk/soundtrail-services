using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

public interface ISaveMusicTrackCatalogProjectionPort
{
    Task SaveAsync(
        ArtistId artistId,
        int version,
        ArtistCatalog aggregate,
        CancellationToken cancellationToken);
}
