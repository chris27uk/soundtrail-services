using Soundtrail.Contracts;
using Soundtrail.Contracts.Api;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;

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
        var riskBand = ToRiskBand(request.RiskScore);

        return new RankedMusicCandidate(
            MusicCatalogId: musicCatalogId,
            RequestCount: 1,
            HighestTrustLevelSeen: request.TrustLevel,
            RiskScore: request.RiskScore,
            Status: riskBand == RiskBand.Blocked ? RankedMusicCandidateStatus.Ignored : RankedMusicCandidateStatus.Pending,
            NextEligibleAt: null);
    }

    public RankedMusicCandidate AcceptNewRequest(LookupMusicRequest request)
    {
        var updatedRiskScore = Math.Max(RiskScore, request.RiskScore);

        return this with
        {
            RequestCount = RequestCount + 1,
            HighestTrustLevelSeen = Math.Max(HighestTrustLevelSeen, request.TrustLevel),
            RiskScore = updatedRiskScore,
            Status = PromoteStatus(Status, ToRiskBand(updatedRiskScore))
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

    private static RankedMusicCandidateStatus PromoteStatus(
        RankedMusicCandidateStatus currentStatus,
        RiskBand riskBand)
    {
        if (currentStatus != RankedMusicCandidateStatus.Pending)
        {
            return currentStatus;
        }

        return riskBand == RiskBand.Blocked
            ? RankedMusicCandidateStatus.Ignored
            : RankedMusicCandidateStatus.Pending;
    }
}
