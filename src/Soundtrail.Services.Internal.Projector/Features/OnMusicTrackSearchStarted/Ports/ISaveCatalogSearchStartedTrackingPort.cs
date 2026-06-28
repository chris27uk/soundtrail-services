using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Ports;

public interface ISaveCatalogSearchCandidateTrackingPort
{
    Task SaveAsync(
        MusicSearchCriteria searchCriteria,
        MusicCatalogId musicCatalogId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken);
}
