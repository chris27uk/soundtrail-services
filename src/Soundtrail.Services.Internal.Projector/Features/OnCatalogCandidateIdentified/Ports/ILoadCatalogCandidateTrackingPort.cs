using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Ports;

public interface ILoadCatalogCandidateTrackingPort
{
    Task<CatalogCandidateTracking?> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
