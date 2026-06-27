using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;

public interface IGetMusicTrackReference
{
    Task<IReadOnlyList<ExternalReference>> GetReferenceToMusicTrack(MusicSearchCriteria searchCriteria, CancellationToken cancellationToken);
}
