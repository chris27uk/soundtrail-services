using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.ProjectionModel;

public interface ILoadMusicTrackCatalogProjectionPort
{
    Task<MusicTrackCatalogProjection> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
