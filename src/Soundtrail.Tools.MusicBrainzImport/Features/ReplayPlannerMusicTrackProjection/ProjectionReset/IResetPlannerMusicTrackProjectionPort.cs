using Soundtrail.Contracts.Common;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection;

public interface IResetPlannerMusicTrackProjectionPort
{
    Task ResetAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
