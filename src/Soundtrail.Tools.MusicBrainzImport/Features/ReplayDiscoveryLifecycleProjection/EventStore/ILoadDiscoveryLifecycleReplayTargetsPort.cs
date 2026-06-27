using Soundtrail.Domain.Search;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayDiscoveryLifecycleProjection.EventStore;

public interface ILoadDiscoveryLifecycleReplayTargetsPort
{
    Task<IReadOnlyList<MusicSearchCriteria>> LoadAsync(CancellationToken cancellationToken);
}
