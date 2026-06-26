using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;

public interface ISaveCatalogSearchStartedTrackingPort
{
    Task SaveAsync(
        CatalogSearchCriteria criteria,
        MusicCatalogId musicCatalogId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken);
}
