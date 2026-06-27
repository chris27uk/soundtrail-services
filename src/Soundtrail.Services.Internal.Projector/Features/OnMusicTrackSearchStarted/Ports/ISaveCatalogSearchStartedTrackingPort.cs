using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;

public interface ISaveCatalogSearchStartedTrackingPort
{
    Task SaveAsync(
        MusicSearchCriteria searchCriteria,
        MusicCatalogId musicCatalogId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken);
}
