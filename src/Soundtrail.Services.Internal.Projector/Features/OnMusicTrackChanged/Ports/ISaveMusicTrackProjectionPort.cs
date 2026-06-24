using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Ports;

public interface ISaveMusicTrackProjectionPort
{
    Task SaveAsync(
        MusicCatalogId musicCatalogId,
        MusicTrackProjection projection,
        CancellationToken cancellationToken);
}
