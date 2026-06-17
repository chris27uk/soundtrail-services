using Soundtrail.Domain.Model;
using Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.TrackSearch
{
    internal sealed class FakeTrackSearchPort : ITrackSearchPort
    {
        private readonly List<SearchResult> results = [];

        public void Seed(params SearchResult[] seededResults)
        {
            this.results.Clear();
            this.results.AddRange(seededResults);
        }

        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            NormalizedSearchQuery query,
            Limit limit,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<SearchResult> matches = this.results
                .Where(track => NormalizedSearchQuery.FromText($"{track.Title.Value} {track.Artist.Value}")
                    .Value.Contains(query.Value, StringComparison.Ordinal))
                .Take(limit.Value)
                .Select(track => new SearchResult(
                    track.Title,
                    track.Artist,
                    track.Isrc,
                    track.Mbid,
                    track.AppleId,
                    track.SpotifyId,
                    ConfidenceScore.From(0.95)))
                .ToArray();

            return Task.FromResult(matches);
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }
}