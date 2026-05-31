using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;

public interface IMusicCatalogSearch
{
    Task<MusicCatalogId?> SearchAsync(NormalizedSearchQuery query, CancellationToken cancellationToken);
}
