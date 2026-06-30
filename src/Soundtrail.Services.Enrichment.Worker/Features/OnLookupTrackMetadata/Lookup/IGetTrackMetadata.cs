using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Lookup;

public interface IGetTrackMetadata
{
    Task<SongMetadata?> GetMetadataAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken);
}
