using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Model;

public interface ILoadMusicTrackProjectionPort
{
    Task<MusicTrackProjection> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
