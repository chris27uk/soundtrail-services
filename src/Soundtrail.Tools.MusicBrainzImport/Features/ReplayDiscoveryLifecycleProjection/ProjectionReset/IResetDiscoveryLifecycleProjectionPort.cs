using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Tools.MusicBrainzImport.Features.OnReplayCatalogSearchStatus;

public interface IResetDiscoveryLifecycleProjectionPort
{
    Task ResetAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
