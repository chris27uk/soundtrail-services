using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Search.Queueing;

namespace Soundtrail.Services.Features.Search;

public sealed class SearchMusicHandler(
    ITrackSearchPort trackSearch,
    IEnqueueMusicRequest enqueueMusicRequest)
{
    public async Task<SearchMusicResponse> Handle(
        SearchMusicRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizedSearchQuery.From(request.Query);
        var results = await trackSearch.SearchAsync(normalizedQuery, request.Limit, cancellationToken);
        var filteredResults = request.MinConfidence is null ? results : results.Where(result => result.Confidence.Value >= request.MinConfidence.Value.Value).ToArray();

        if (filteredResults.Count > 0)
        {
            return SearchMusicResponse.Resolved(request.Query, filteredResults);
        }

        await enqueueMusicRequest.EnqueueAsync(
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
