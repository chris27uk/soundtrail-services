namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

public interface ISaveMusicTrackCatalogProjectionPort
{
    Task SaveAsync(
        ArtistCatalogProjection projection,
        CancellationToken cancellationToken);
}
