using Soundtrail.Contracts;
using Soundtrail.Domain;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.TrackSearch;

namespace Soundtrail.Services.Api.Features.Search;

public sealed class SearchMusicHandler(ITrackSearchPort trackSearch, IEnqueueMusicRequest enqueueMusicRequest) : IHandler<SearchMusicRequest, SearchMusicResponse>
{
    public async Task<SearchMusicResponse> Handle(SearchMusicRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = request.Query;
        var results = await trackSearch.SearchAsync(normalizedQuery, request.Limit, cancellationToken);
        var filteredResults = request.MinConfidence is null ? results : results.Where(result => result.Confidence.Value >= request.MinConfidence.Value.Value).ToArray();

        if (filteredResults.Count > 0)
        {
            return SearchMusicResponse.Resolved(request.Query, filteredResults);
        }

        await enqueueMusicRequest.EnqueueAsync(normalizedQuery.ToNewLookupRequest(), cancellationToken);
        return SearchMusicResponse.Pending(request.Query);
    }
}
