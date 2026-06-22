using Soundtrail.Domain.Discovery;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;

public interface IResetDiscoveryLifecycleProjectionPort
{
    Task ResetAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
