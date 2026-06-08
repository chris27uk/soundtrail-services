using Soundtrail.Services.Api.Features.Search.TrackSearch;

namespace Soundtrail.Services.Tests.Unit.Api.Infrastructure
{
    internal sealed class FakeTrackSearchPort(params SearchResult[] results) : ITrackSearchPort
    {
        private readonly IReadOnlyList<SearchResult> searchResults = results;

        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            NormalizedSearchQuery query,
            Limit limit,
            CancellationToken cancellationToken)
        {
            var matches = this.searchResults
                .Where(track => NormalizedSearchQuery.FromText($"{track.Title.Value} {track.Artist.Value}")
                    .Value.Contains(query.Value, StringComparison.Ordinal))
                .Take(limit.Value)
                .ToArray();

            return Task.FromResult<IReadOnlyList<SearchResult>>(matches);
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }
}