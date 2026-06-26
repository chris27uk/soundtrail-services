using Soundtrail.Contracts.Common;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;

public interface ILoadCatalogSearchStatusMusicTrackPort
{
    Task<CatalogSearchStatusMusicTrack?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
