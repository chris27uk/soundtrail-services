using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.GetReference;

public interface IGetMusicTrackReference
{
    Task<IReadOnlyList<ExternalReference>> GetReferenceToMusicTrack(MusicSearchTerm searchTerm, CancellationToken cancellationToken);
}
