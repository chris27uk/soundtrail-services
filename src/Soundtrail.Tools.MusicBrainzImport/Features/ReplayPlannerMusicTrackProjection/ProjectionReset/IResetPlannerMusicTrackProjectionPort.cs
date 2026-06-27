using Soundtrail.Contracts.Common;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayPlannerMusicTrackProjection.ProjectionReset;

public interface IResetPlannerMusicTrackProjectionPort
{
    Task ResetAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken);
}
