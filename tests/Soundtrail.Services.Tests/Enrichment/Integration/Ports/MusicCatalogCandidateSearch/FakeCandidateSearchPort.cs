using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.MusicCatalogCandidateSearch
{
    internal sealed class FakeCandidateSearchPort : IMusicCatalogCandidateSearch
    {
        private readonly List<(string MusicCatalogId, string SearchText)> entries = [];

        public void Seed(string musicCatalogId, string searchText) => this.entries.Add((musicCatalogId, searchText));

        public Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(
            NormalizedSearchQuery query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<MusicCatalogMatch> matches = this.entries
                .Where(entry => entry.SearchText.Contains(query.Value, StringComparison.Ordinal))
                .Select(entry => new MusicCatalogMatch(MusicCatalogId.From(entry.MusicCatalogId), 1.00m))
                .ToArray();

            return Task.FromResult(matches);
        }
    }
}
