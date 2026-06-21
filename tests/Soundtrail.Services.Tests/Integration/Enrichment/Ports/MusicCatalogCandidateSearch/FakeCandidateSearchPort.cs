using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicCatalogCandidateSearch
{
    internal sealed class FakeCandidateSearchPort : IMusicCatalogCandidateSearch
    {
        private readonly List<Entry> entries = [];

        public void Seed(
            string musicCatalogId,
            string searchText,
            string? title = null,
            string? artist = null,
            string? albumTitle = null,
            string? isrc = null,
            string? mbid = null,
            DateOnly? releaseDate = null) =>
            this.entries.Add(new Entry(
                musicCatalogId,
                searchText,
                title,
                artist,
                albumTitle,
                isrc,
                mbid,
                releaseDate));

        public Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(
            NormalizedSearchQuery query,
            CancellationToken cancellationToken)
        {
            var compactQuery = Domain.Model.MusicIdentityText.NormalizeCompact(query.Value);
            var exactIdentity = this.entries
                .Where(entry =>
                    Domain.Model.MusicIdentityText.NormalizeCompact(entry.Isrc) == compactQuery
                    || Domain.Model.MusicIdentityText.NormalizeCompact(entry.Mbid) == compactQuery)
                .Select(entry => new MusicCatalogMatch(
                    MusicCatalogId.From(entry.MusicCatalogId),
                    1.00m,
                    BuildEvidence(entry, isExactIdentityMatch: true)))
                .ToArray();

            if (exactIdentity.Length > 0)
            {
                return Task.FromResult<IReadOnlyList<MusicCatalogMatch>>(exactIdentity);
            }

            IReadOnlyList<MusicCatalogMatch> matches = this.entries
                .Where(entry => BuildSearchableText(entry).Contains(query.Value, StringComparison.Ordinal))
                .Select(entry => new MusicCatalogMatch(
                    MusicCatalogId.From(entry.MusicCatalogId),
                    string.Equals(BuildSearchableText(entry), query.Value, StringComparison.Ordinal) ? 1.00m : 0.90m,
                    BuildEvidence(entry, isExactIdentityMatch: false)))
                .ToArray();

            return Task.FromResult(matches);
        }

        private static MusicCatalogMatchEvidence BuildEvidence(
            Entry entry,
            bool isExactIdentityMatch) =>
            new(
                isExactIdentityMatch,
                Domain.Model.MusicIdentityText.NormalizeFreeText(entry.Title),
                Domain.Model.MusicIdentityText.NormalizeFreeText(entry.Artist),
                Domain.Model.MusicIdentityText.NormalizeFreeText(entry.AlbumTitle),
                Domain.Model.MusicIdentityText.NormalizeCompact(entry.Isrc),
                Domain.Model.MusicIdentityText.NormalizeCompact(entry.Mbid),
                entry.ReleaseDate);

        private static string BuildSearchableText(Entry entry) =>
            string.Join(
                ' ',
                new[]
                {
                    entry.SearchText,
                    Domain.Model.MusicIdentityText.NormalizeFreeText(entry.AlbumTitle)
                }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        private sealed record Entry(
            string MusicCatalogId,
            string SearchText,
            string? Title,
            string? Artist,
            string? AlbumTitle,
            string? Isrc,
            string? Mbid,
            DateOnly? ReleaseDate);
    }
}
