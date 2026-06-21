using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

public sealed class MusicCatalogMatchResolver
{
    private const decimal MinimumAcceptedScore = 0.80m;
    private const decimal MinimumWinningMargin = 0.10m;

    public MusicCatalogResolution Resolve(
        IReadOnlyList<MusicCatalogMatch> matches,
        MusicCatalogResolutionContext? context = null)
    {
        context ??= MusicCatalogResolutionContext.Empty;

        if (!string.IsNullOrWhiteSpace(context.NormalizedQuery))
        {
            var exactQueryMatches = matches
                .Where(match => MatchesExactQuery(match, context.NormalizedQuery))
                .OrderByDescending(static match => match.Score)
                .Take(2)
                .ToArray();

            if (exactQueryMatches.Length == 1)
            {
                return MusicCatalogResolution.Resolved(exactQueryMatches[0].MusicCatalogId);
            }

            if (exactQueryMatches.Length > 1)
            {
                return MusicCatalogResolution.Ambiguous();
            }
        }

        var exactIdentityMatches = matches
            .Where(static match => match.HasExactIdentityMatch())
            .OrderByDescending(static match => match.Score)
            .ToArray();

        if (context.ReleaseDate is not null)
        {
            var releaseDateMatches = exactIdentityMatches
                .Where(match => match.Evidence.ReleaseDate == context.ReleaseDate)
                .OrderByDescending(static match => match.Score)
                .Take(2)
                .ToArray();

            if (releaseDateMatches.Length == 1)
            {
                return MusicCatalogResolution.Resolved(releaseDateMatches[0].MusicCatalogId);
            }

            if (releaseDateMatches.Length > 1)
            {
                return MusicCatalogResolution.Ambiguous();
            }
        }

        if (exactIdentityMatches.Length == 1)
        {
            return MusicCatalogResolution.Resolved(exactIdentityMatches[0].MusicCatalogId);
        }

        if (exactIdentityMatches.Length > 1)
        {
            return MusicCatalogResolution.Ambiguous();
        }

        var topMatches = matches
            .OrderByDescending(static match => match.Score)
            .Take(2)
            .ToArray();

        if (topMatches.Length == 0)
        {
            return MusicCatalogResolution.NotFound();
        }

        var bestMatch = topMatches[0];
        if (!bestMatch.MeetsMinimumScore(MinimumAcceptedScore))
        {
            return MusicCatalogResolution.NotFound();
        }

        if (topMatches.Length == 1)
        {
            return MusicCatalogResolution.Resolved(bestMatch.MusicCatalogId);
        }

        if (!bestMatch.HasWinningMarginOver(topMatches[1], MinimumWinningMargin))
        {
            return MusicCatalogResolution.Ambiguous();
        }

        return MusicCatalogResolution.Resolved(bestMatch.MusicCatalogId);
    }

    private static bool MatchesExactQuery(MusicCatalogMatch match, string normalizedQuery)
    {
        var evidence = match.Evidence;
        if (string.IsNullOrWhiteSpace(evidence.NormalizedTitle)
            || string.IsNullOrWhiteSpace(evidence.NormalizedArtist))
        {
            return false;
        }

        var titleArtist = MusicIdentityText.NormalizeFreeText($"{evidence.NormalizedTitle} {evidence.NormalizedArtist}");
        if (string.Equals(titleArtist, normalizedQuery, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(evidence.NormalizedAlbumTitle))
        {
            return false;
        }

        var titleArtistAlbum = MusicIdentityText.NormalizeFreeText(
            $"{evidence.NormalizedTitle} {evidence.NormalizedArtist} {evidence.NormalizedAlbumTitle}");
        return string.Equals(titleArtistAlbum, normalizedQuery, StringComparison.Ordinal);
    }
}
