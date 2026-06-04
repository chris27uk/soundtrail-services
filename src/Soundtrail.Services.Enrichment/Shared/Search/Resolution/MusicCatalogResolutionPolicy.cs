namespace Soundtrail.Services.Enrichment.Shared.Search.Resolution;

public sealed class MusicCatalogResolutionPolicy
{
    private const decimal MinimumAcceptedScore = 0.80m;
    private const decimal MinimumWinningMargin = 0.10m;

    public MusicCatalogResolution Resolve(IReadOnlyList<MusicCatalogMatch> matches)
    {
        if (matches.Count == 0)
        {
            return MusicCatalogResolution.NotFound();
        }

        var orderedMatches = matches
            .OrderByDescending(static x => x.Score)
            .ToArray();

        var bestMatch = orderedMatches[0];
        if (bestMatch.Score < MinimumAcceptedScore)
        {
            return MusicCatalogResolution.NotFound();
        }

        if (orderedMatches.Length == 1)
        {
            return MusicCatalogResolution.Resolved(bestMatch.MusicCatalogId);
        }

        var secondBestMatch = orderedMatches[1];
        if (bestMatch.Score - secondBestMatch.Score < MinimumWinningMargin)
        {
            return MusicCatalogResolution.Ambiguous();
        }

        return MusicCatalogResolution.Resolved(bestMatch.MusicCatalogId);
    }
}
