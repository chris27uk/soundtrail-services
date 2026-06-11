using Soundtrail.Domain.Responses;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;

public interface IGetMusicTrackReference
{
    Task<IReadOnlyList<ExternalReference>> GetReferenceToMusicTrack(MusicSearchTerm searchTerm, CancellationToken cancellationToken);
}
