using Soundtrail.Domain.Model;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Ports;

public interface ILoadCatalogSearchPlannedTrackingPort
{
    Task<CatalogSearchPlannedTracking?> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
