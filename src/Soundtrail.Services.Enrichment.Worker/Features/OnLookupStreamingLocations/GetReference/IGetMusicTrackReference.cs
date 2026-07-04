using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;

public interface IGetMusicTrackReference
{
    Task<IReadOnlyList<ExternalReference>> GetReferenceToMusicTrack(LookupCriteria searchCriteria, CancellationToken cancellationToken);
}
