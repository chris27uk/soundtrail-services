using Soundtrail.Services.Application.Search;
using Soundtrail.Services.Domain.ValueTypes;

namespace Soundtrail.Services.Application.Ports;

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
