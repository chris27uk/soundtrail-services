using Soundtrail.Contracts.Common;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;

public interface ILoadPotentialCatalogLookupWorkPort
{
    Task<PotentialCatalogLookupWorkState> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
