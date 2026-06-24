using Soundtrail.Domain.Discovery;

namespace Soundtrail.Tools.MusicBrainzImport.Features.OnReplayCatalogSearchStatus;

public interface ILoadDiscoveryLifecycleReplayTargetsPort
{
    Task<IReadOnlyList<CatalogSearchCriteria>> LoadAsync(CancellationToken cancellationToken);
}
