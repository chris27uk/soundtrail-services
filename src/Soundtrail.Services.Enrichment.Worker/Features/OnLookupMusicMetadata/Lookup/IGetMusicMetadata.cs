using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Lookup;

public interface IGetTrackMetadata
{
    Task<SongMetadata?> GetMetadataAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
