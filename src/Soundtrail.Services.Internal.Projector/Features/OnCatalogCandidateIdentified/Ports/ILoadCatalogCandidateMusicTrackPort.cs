using Soundtrail.Contracts.Common;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Ports;

public interface ILoadCatalogCandidateMusicTrackPort
{
    Task<CatalogCandidateMusicTrack?> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
