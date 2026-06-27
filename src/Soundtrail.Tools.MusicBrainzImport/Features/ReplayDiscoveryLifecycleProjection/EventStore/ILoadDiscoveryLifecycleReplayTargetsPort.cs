using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Tools.MusicBrainzImport.Features.OnReplayCatalogSearchStatus;

public interface ILoadDiscoveryLifecycleReplayTargetsPort
{
    Task<IReadOnlyList<MusicSearchCriteria>> LoadAsync(CancellationToken cancellationToken);
}
