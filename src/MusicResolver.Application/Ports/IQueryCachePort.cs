using MusicResolver.Application.Search;
using MusicResolver.Domain.ValueTypes;

namespace MusicResolver.Application.Ports;

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
