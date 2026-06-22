using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Indexes;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;

public sealed class RavenMusicCatalogCandidateSearch(IDocumentStore documentStore) : IMusicCatalogCandidateSearch
{
    public async Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var exactIdentityMatches = await SearchByExactIdentityAsync(session, query, cancellationToken);
        if (exactIdentityMatches.Count > 0)
        {
            return exactIdentityMatches;
        }

        var documents = await session
            .Query<RavenTrackRecordDto, TrackCatalogue_BySearchText>()
            .Search(x => x.SearchText, query.Value)
            .Take(5)
            .ToListAsync(cancellationToken);

        return documents
            .Select(document => new MusicCatalogMatch(
                MusicCatalogId.From(document.Id.Replace("track-catalogue/", string.Empty)),
                Score(document, query.Value),
                BuildEvidence(document, isExactIdentityMatch: false)))
            .OrderByDescending(match => match.Score)
            .ToArray();
    }

    private static async Task<IReadOnlyList<MusicCatalogMatch>> SearchByExactIdentityAsync(
        IAsyncDocumentSession session,
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        var compactQuery = MusicIdentityText.NormalizeCompact(query.Value);
        if (!MusicIdentityText.LooksLikeIsrc(compactQuery)
            && !MusicIdentityText.LooksLikeMusicBrainzId(compactQuery))
        {
            return [];
        }

        var documents = await session
            .Query<RavenTrackRecordDto, TrackCatalogue_BySearchText>()
            .Where(document =>
                document.NormalizedIsrc == compactQuery
                || document.NormalizedMbid == compactQuery)
            .Take(5)
            .ToListAsync(cancellationToken);

        return documents
            .Select(document => new MusicCatalogMatch(
                MusicCatalogId.From(document.Id.Replace("track-catalogue/", string.Empty)),
                1.00m,
                BuildEvidence(document, isExactIdentityMatch: true)))
            .ToArray();
    }

    private static MusicCatalogMatchEvidence BuildEvidence(
        RavenTrackRecordDto document,
        bool isExactIdentityMatch) =>
        new(
            isExactIdentityMatch,
            MusicIdentityText.NormalizeFreeText(document.Title),
            document.NormalizedArtist,
            document.NormalizedAlbumTitle,
            document.NormalizedIsrc,
            document.NormalizedMbid,
            document.ReleaseDate);

    private static decimal Score(RavenTrackRecordDto document, string query)
    {
        var searchableText = BuildSearchableText(document);

        if (string.Equals(searchableText, query, StringComparison.Ordinal))
        {
            return 1.00m;
        }

        if (string.Equals(MusicIdentityText.NormalizeFreeText(document.Title), query, StringComparison.Ordinal))
        {
            return 0.96m;
        }

        if (searchableText.Contains(query, StringComparison.Ordinal))
        {
            return 0.90m;
        }

        var overlap = TokenOverlap(document, query);
        if (overlap >= 0.75m)
        {
            return 0.85m;
        }

        if (overlap >= 0.50m)
        {
            return 0.80m;
        }

        return 0.79m;
    }

    private static decimal TokenOverlap(RavenTrackRecordDto document, string query)
    {
        var queryTokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (queryTokens.Length == 0)
        {
            return 0m;
        }

        var candidate = BuildSearchableText(document);

        var matchedCount = queryTokens.Count(token => candidate.Contains(token, StringComparison.Ordinal));
        return matchedCount / (decimal)queryTokens.Length;
    }

    private static string BuildSearchableText(RavenTrackRecordDto document) =>
        string.Join(
            ' ',
            new[]
            {
                document.SearchText,
                document.NormalizedAlbumTitle
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));
}
