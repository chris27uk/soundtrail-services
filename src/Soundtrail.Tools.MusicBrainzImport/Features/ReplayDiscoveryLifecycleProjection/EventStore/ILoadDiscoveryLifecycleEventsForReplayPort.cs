using Soundtrail.Domain.Discovery;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection;

public interface ILoadDiscoveryLifecycleEventsForReplayPort
{
    Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
