namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record RankedMusicCandidate(
    MusicCatalogId MusicCatalogId,
    int RequestCount,
    int HighestTrustLevelSeen,
    int RiskScore,
    RankedMusicCandidateStatus Status,
    DateTimeOffset? NextEligibleAt)
{
    public RiskBand RiskBand => ToRiskBand(RiskScore);

    public bool IsSuspicious => RiskBand is RiskBand.High or RiskBand.Blocked;

    public bool IsPending => Status == RankedMusicCandidateStatus.Pending;

    public bool IsEligibleAt(DateTimeOffset when) => NextEligibleAt is null || NextEligibleAt <= when;

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

    public RankedMusicCandidate AcceptNewRequest(LookupMusicRequest request)
    {
        return this with
        {
            RequestCount = RequestCount + 1,
            HighestTrustLevelSeen = Math.Max(HighestTrustLevelSeen, request.TrustLevel),
            RiskScore = Math.Max(RiskScore, request.RiskScore)
        };
    }

    private static RiskBand ToRiskBand(int riskScore)
    {
        return riskScore switch
        {
            >= 90 => RiskBand.Blocked,
            >= 60 => RiskBand.High,
            >= 30 => RiskBand.Medium,
            _ => RiskBand.Low
        };
    }
}
