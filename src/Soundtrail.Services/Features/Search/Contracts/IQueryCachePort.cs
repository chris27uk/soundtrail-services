using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Features.Search.Contracts;

public interface IQueryCachePort
{
    Task<SearchMusicResponse?> GetAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken);

    Task StoreAsync(
        NormalizedSearchQuery query,
        SearchMusicResponse response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken);

    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
