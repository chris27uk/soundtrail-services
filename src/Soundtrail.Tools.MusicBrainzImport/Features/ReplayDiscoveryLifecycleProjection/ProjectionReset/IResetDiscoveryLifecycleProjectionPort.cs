using Soundtrail.Domain.Search;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.ProjectionReset;

public interface IResetDiscoveryLifecycleProjectionPort
{
    Task ResetAsync(
        LookupCriteria searchCriteria,
        CancellationToken cancellationToken);
}
