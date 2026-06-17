using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

public sealed record MusicCatalogMatch(MusicCatalogId MusicCatalogId, decimal Score)
{
    public bool MeetsMinimumScore(decimal minimumScore) =>
        Score >= minimumScore;

    public bool HasWinningMarginOver(MusicCatalogMatch other, decimal minimumMargin) =>
        Score - other.Score >= minimumMargin;
}
