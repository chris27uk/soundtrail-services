using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;

public interface IGetMusicTrackReference
{
    Task<IReadOnlyList<StreamingLocation>> GetStreamingLocations(LookupCriteria searchCriteria, CancellationToken cancellationToken);
}
