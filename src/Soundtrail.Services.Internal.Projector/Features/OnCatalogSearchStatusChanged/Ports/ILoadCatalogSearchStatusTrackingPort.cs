using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;

public interface ILoadCatalogSearchStatusTrackingPort
{
    Task<CatalogSearchStatusTracking?> LoadAsync(
        LookupCriteria searchCriteria,
        CancellationToken cancellationToken);
}
