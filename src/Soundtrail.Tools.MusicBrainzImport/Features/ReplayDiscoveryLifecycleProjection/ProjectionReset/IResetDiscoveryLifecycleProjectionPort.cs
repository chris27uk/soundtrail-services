using Soundtrail.Domain.Discovery;

namespace Soundtrail.Tools.MusicBrainzImport.Features.OnReplayCatalogSearchStatus;

public interface IResetDiscoveryLifecycleProjectionPort
{
    Task ResetAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
