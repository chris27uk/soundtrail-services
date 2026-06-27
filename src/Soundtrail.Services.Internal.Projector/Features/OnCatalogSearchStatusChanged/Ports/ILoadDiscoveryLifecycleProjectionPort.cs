using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;

public interface ILoadDiscoveryLifecycleProjectionPort
{
    Task<DiscoveryLifecycleProjection> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
