using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Tools.MusicBrainzImport.Features.OnReplayCatalogSearchStatus;

public interface ILoadDiscoveryLifecycleEventsForReplayPort
{
    Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
