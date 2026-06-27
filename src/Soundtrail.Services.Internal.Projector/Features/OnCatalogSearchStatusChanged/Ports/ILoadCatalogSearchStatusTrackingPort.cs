using Soundtrail.Domain.Model;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Ports;

public interface ILoadCatalogSearchStatusTrackingPort
{
    Task<CatalogSearchStatusTracking?> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
