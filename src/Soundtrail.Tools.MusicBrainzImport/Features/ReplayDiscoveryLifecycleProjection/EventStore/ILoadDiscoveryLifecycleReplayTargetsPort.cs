using Soundtrail.Domain.Discovery;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;

public interface ILoadDiscoveryLifecycleReplayTargetsPort
{
    Task<IReadOnlyList<CatalogSearchCriteria>> LoadAsync(CancellationToken cancellationToken);
}
