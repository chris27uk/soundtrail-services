using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Search.Queueing;

namespace Soundtrail.Services.Features.Search;

public sealed class SearchMusicHandler(
    IQueryCachePort queryCache,
    ITrackSearchPort trackSearch,
    ILookupMusicRequestQueue lookupMusicRequestQueue)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<SearchMusicResponse> Handle(
        SearchMusicRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizedSearchQuery.From(request.Query);
        var shouldUseCache = request.MinConfidence is null;

        if (shouldUseCache)
        {
            var cached = await queryCache.GetAsync(normalizedQuery, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var results = await trackSearch.SearchAsync(
            normalizedQuery,
            request.Limit,
            cancellationToken);

        var filteredResults = request.MinConfidence is null
            ? results
            : results
                .Where(result => result.Confidence.Value >= request.MinConfidence.Value.Value)
                .ToArray();

        if (filteredResults.Count > 0)
        {
            var resolved = SearchMusicResponse.Resolved(request.Query, filteredResults);

            if (shouldUseCache)
            {
                await queryCache.StoreAsync(
                    normalizedQuery,
                    resolved,
                    CacheTtl,
                    cancellationToken);
            }

            return resolved;
        }

        await lookupMusicRequestQueue.EnqueueAsync(
            new LookupMusicRequest(
                normalizedQuery,
                TrustLevel: 0,
                RiskScore: 0,
                OccurredAt: DateTimeOffset.UtcNow,
                CorrelationId: Guid.NewGuid().ToString("N")),
            cancellationToken);

        return SearchMusicResponse.Pending(request.Query);
    }
}
