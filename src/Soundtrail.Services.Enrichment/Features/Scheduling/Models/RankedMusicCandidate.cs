using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record RankedMusicCandidate(
    MusicCatalogId MusicCatalogId,
    int RequestCount,
    int HighestTrustLevelSeen,
    int RiskScore,
    RankedMusicCandidateStatus Status,
    DateTimeOffset? NextEligibleAt)
{
    public bool IsSuspicious => RiskScore >= 100;

    public bool IsPending => Status == RankedMusicCandidateStatus.Pending;

    public bool IsEligibleAt(DateTimeOffset when) =>
        NextEligibleAt is null || NextEligibleAt <= when;

    public static RankedMusicCandidate Create(LookupMusicRequest request, MusicCatalogId musicCatalogId)
    {
        return new RankedMusicCandidate(
            MusicCatalogId: musicCatalogId,
            RequestCount: 1,
            HighestTrustLevelSeen: request.TrustLevel,
            RiskScore: request.RiskScore,
            Status: RankedMusicCandidateStatus.Pending,
            NextEligibleAt: null);
    }

    public RankedMusicCandidate Register(LookupMusicRequest request)
    {
        return this with
        {
            RequestCount = RequestCount + 1,
            HighestTrustLevelSeen = Math.Max(HighestTrustLevelSeen, request.TrustLevel),
            RiskScore = Math.Max(RiskScore, request.RiskScore)
        };
    }
}
