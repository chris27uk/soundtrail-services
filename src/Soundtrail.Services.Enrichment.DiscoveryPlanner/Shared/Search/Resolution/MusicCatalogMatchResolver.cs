namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

public sealed class MusicCatalogMatchResolver
{
    private const decimal MinimumAcceptedScore = 0.80m;
    private const decimal MinimumWinningMargin = 0.10m;

    public MusicCatalogResolution Resolve(IReadOnlyList<MusicCatalogMatch> matches)
    {
        var exactIdentityMatches = matches
            .Where(static match => match.HasExactIdentityMatch())
            .OrderByDescending(static match => match.Score)
            .Take(2)
            .ToArray();

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
}
