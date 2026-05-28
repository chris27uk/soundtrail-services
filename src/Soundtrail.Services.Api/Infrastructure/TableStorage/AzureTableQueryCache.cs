using System.Collections.Concurrent;
using Soundtrail.Services.Application.Ports;
using Soundtrail.Services.Application.Search;
using Soundtrail.Services.Domain.ValueTypes;

namespace Soundtrail.Services.Api.Infrastructure.TableStorage;

public sealed class AzureTableQueryCache : IQueryCachePort
{
    private readonly ConcurrentDictionary<string, SearchMusicResponse> _responses = new();

    public Task<SearchMusicResponse?> GetAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        _responses.TryGetValue(query.Value, out var response);
        return Task.FromResult(response);
    }

    public Task StoreAsync(
        NormalizedSearchQuery query,
        SearchMusicResponse response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        _responses[query.Value] = response;
        return Task.CompletedTask;
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}
