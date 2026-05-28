using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Features.Search;

public sealed class SearchMusicHandler
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly IQueryCachePort _queryCache;
    private readonly ITrackSearchPort _trackSearch;
    private readonly IResolutionDemandPort _resolutionDemand;

    public SearchMusicHandler(
        IQueryCachePort queryCache,
        ITrackSearchPort trackSearch,
        IResolutionDemandPort resolutionDemand)
    {
        _queryCache = queryCache;
        _trackSearch = trackSearch;
        _resolutionDemand = resolutionDemand;
    }

    public async Task<SearchMusicResponse> Handle(
        SearchMusicRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizedSearchQuery.From(request.Query);

        var cached = await _queryCache.GetAsync(normalizedQuery, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var results = await _trackSearch.SearchAsync(
            normalizedQuery,
            request.Limit,
            cancellationToken);

        if (results.Count > 0)
        {
            var resolved = SearchMusicResponse.Resolved(request.Query, results);

            await _queryCache.StoreAsync(
                normalizedQuery,
                resolved,
                CacheTtl,
                cancellationToken);

            return resolved;
        }

        var queryId = await _resolutionDemand.RecordDemandAsync(
            normalizedQuery,
            cancellationToken);

        return SearchMusicResponse.Pending(request.Query, queryId);
    }
}
