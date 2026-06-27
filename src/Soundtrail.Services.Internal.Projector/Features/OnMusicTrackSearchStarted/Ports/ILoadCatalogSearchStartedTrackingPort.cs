using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;

public interface ILoadCatalogSearchStartedTrackingPort
{
    Task<CatalogSearchStartedTracking?> LoadAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken);
}
