using Soundtrail.Domain.Responses;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;

public interface IPlaybackReferenceSource
{
    Task<IReadOnlyList<ExternalReference>> GetPlaybackReferencesAsync(
        PlaybackReferenceLookupKey lookupKey,
        CancellationToken cancellationToken);
}
