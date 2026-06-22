using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Model;

public interface ISaveMusicTrackProjectionPort
{
    Task SaveAsync(
        MusicCatalogId musicCatalogId,
        MusicTrackProjection projection,
        CancellationToken cancellationToken);
}
