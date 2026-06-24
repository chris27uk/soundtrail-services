using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

public interface ILoadMusicTrackCatalogProjectionPort
{
    Task<MusicTrackCatalogProjection> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
