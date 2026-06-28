using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Ports;

public interface ISaveCatalogCandidateTrackingPort
{
    Task SaveAsync(
        MusicSearchCriteria searchCriteria,
        MusicCatalogId musicCatalogId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken);
}
