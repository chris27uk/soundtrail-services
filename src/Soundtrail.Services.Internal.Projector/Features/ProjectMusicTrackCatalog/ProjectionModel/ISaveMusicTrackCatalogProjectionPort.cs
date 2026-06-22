namespace Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackCatalog.ProjectionModel;

public interface ISaveMusicTrackCatalogProjectionPort
{
    Task SaveAsync(
        MusicTrackCatalogProjection projection,
        CancellationToken cancellationToken);
}
