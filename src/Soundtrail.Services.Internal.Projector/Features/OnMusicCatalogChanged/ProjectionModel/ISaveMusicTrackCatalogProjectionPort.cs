namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

public interface ISaveMusicTrackCatalogProjectionPort
{
    Task SaveAsync(
        MusicTrackCatalogProjection projection,
        CancellationToken cancellationToken);
}
