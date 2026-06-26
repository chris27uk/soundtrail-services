using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Ports;

public interface ILoadCatalogSearchPlannedTrackingPort
{
    Task<CatalogSearchPlannedTracking?> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
