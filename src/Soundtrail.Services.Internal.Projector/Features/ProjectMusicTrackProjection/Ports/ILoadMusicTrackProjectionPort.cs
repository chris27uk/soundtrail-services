using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackProjection.Ports;

public interface ILoadMusicTrackProjectionPort
{
    Task<MusicTrackProjection> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
