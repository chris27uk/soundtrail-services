using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;

public interface ILoadCatalogSearchStartedTrackingPort
{
    Task<CatalogSearchStartedTracking?> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
