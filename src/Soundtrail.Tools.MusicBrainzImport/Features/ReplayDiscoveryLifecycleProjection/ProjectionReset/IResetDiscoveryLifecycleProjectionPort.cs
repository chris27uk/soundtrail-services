using Soundtrail.Domain.Search;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.ProjectionReset;

public interface IResetDiscoveryLifecycleProjectionPort
{
    Task ResetAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
