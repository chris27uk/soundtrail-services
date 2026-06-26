using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;

public interface ILoadCatalogSearchStatusTrackingPort
{
    Task<CatalogSearchStatusTracking?> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
