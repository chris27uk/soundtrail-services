using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Lookup;

public interface IGetAlbumMetadata
{
    Task<Album?> GetMetadataAsync(
        LookupCriteria criteria,
        CancellationToken cancellationToken);
}
