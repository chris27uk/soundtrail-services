using Soundtrail.Contracts.Common;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Ports;

public interface ILoadCatalogSearchCandidateMusicTrackPort
{
    Task<CatalogSearchCandidateMusicTrack?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
