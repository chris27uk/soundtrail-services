using Soundtrail.Contracts.Common;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Ports;

public interface ILoadCatalogSearchPlannedMusicTrackPort
{
    Task<CatalogSearchPlannedMusicTrack?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
