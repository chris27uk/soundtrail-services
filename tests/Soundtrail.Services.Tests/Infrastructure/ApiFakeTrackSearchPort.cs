using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Integration.Features.Search
{
    public sealed class ApiFakeTrackSearchPort : ITrackSearchPort
    {
        private readonly List<SearchResult> _results = new();

        public bool Ready { get; set; } = true;

        public void Seed(params SearchResult[] results)
        {
            this._results.Clear();
            this._results.AddRange(results);
        }

        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            NormalizedSearchQuery query,
            Limit limit,
            CancellationToken cancellationToken)
        {
            var results = this._results
                .Where(track => NormalizedSearchQuery.FromText($"{track.Title.Value} {track.Artist.Value}")
                    .Value.Contains(query.Value, StringComparison.Ordinal))
                .Take(limit.Value)
                .ToArray();

            return Task.FromResult<IReadOnlyList<SearchResult>>(results);
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(Ready);
    }
}
