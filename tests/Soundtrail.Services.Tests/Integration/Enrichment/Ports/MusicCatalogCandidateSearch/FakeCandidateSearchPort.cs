using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

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
            MusicSearchCriteria searchCriteria,
            CancellationToken cancellationToken)
        {
            var normalizedQuery = searchCriteria.Match(
                withQuery: static query => query,
                withTitleAndArtist: static (title, artist, album) => MusicIdentityText.NormalizeFreeText(
                    string.Join(
                        ' ',
                        new[] { title, artist, album }
                            .Where(static value => !string.IsNullOrWhiteSpace(value)))),
                withIsrcAction: static isrc => isrc);

            var compactQuery = MusicIdentityText.NormalizeCompact(normalizedQuery);
            var exactIdentity = this.entries
                .Where(entry =>
                    MusicIdentityText.NormalizeCompact(entry.Isrc) == compactQuery
                    || MusicIdentityText.NormalizeCompact(entry.Mbid) == compactQuery)
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
                .Where(entry => BuildSearchableText(entry).Contains(normalizedQuery, StringComparison.Ordinal))
                .Select(entry => new MusicCatalogMatch(
                    MusicCatalogId.From(entry.MusicCatalogId),
                    string.Equals(BuildSearchableText(entry), normalizedQuery, StringComparison.Ordinal) ? 1.00m : 0.90m,
                    BuildEvidence(entry, isExactIdentityMatch: false)))
                .ToArray();

            return Task.FromResult(matches);
        }

        private static MusicCatalogMatchEvidence BuildEvidence(
            Entry entry,
            bool isExactIdentityMatch) =>
            new(
                isExactIdentityMatch,
                MusicIdentityText.NormalizeFreeText(entry.Title),
                MusicIdentityText.NormalizeFreeText(entry.Artist),
                MusicIdentityText.NormalizeFreeText(entry.AlbumTitle),
                MusicIdentityText.NormalizeCompact(entry.Isrc),
                MusicIdentityText.NormalizeCompact(entry.Mbid),
                entry.ReleaseDate);

        private static string BuildSearchableText(Entry entry) =>
            string.Join(
                ' ',
                new[]
                {
                    entry.SearchText,
                    MusicIdentityText.NormalizeFreeText(entry.AlbumTitle)
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
