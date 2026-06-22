using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

public sealed record MusicCatalogMatch(
    MusicCatalogId MusicCatalogId,
    decimal Score,
    MusicCatalogMatchEvidence Evidence)
{
    public MusicCatalogMatch(MusicCatalogId musicCatalogId, decimal score)
        : this(musicCatalogId, score, MusicCatalogMatchEvidence.None)
    {
    }

    public bool MeetsMinimumScore(decimal minimumScore) =>
        Score >= minimumScore;

    public bool HasWinningMarginOver(MusicCatalogMatch other, decimal minimumMargin) =>
        Score - other.Score >= minimumMargin;

    public bool HasExactIdentityMatch() =>
        Evidence.IsExactIdentityMatch;
}
