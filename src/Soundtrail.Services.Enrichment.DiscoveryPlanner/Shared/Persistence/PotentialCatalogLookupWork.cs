using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;

public sealed record PotentialCatalogLookupWork(
    MusicCatalogId MusicCatalogId,
    int RequestCount,
    int HighestTrustLevelSeen,
    int RiskScore,
    PotentialCatalogLookupWorkStatus Status,
    DateTimeOffset? NextEligibleAt)
{
    public RiskBand RiskBand => ToRiskBand(RiskScore);

    public bool IsSuspicious => RiskBand is RiskBand.High or RiskBand.Blocked;

    public bool IsPending => Status == PotentialCatalogLookupWorkStatus.Pending;

    public bool IsEligibleAt(DateTimeOffset when) => NextEligibleAt is null || NextEligibleAt <= when;

    public static PotentialCatalogLookupWork Create(CatalogSearchAttempt request, MusicCatalogId musicCatalogId)
    {
        var riskBand = ToRiskBand(request.RiskScore);

        return new PotentialCatalogLookupWork(
            MusicCatalogId: musicCatalogId,
            RequestCount: 1,
            HighestTrustLevelSeen: request.TrustLevel,
            RiskScore: request.RiskScore,
            Status: riskBand == RiskBand.Blocked ? PotentialCatalogLookupWorkStatus.Ignored : PotentialCatalogLookupWorkStatus.Pending,
            NextEligibleAt: null);
    }

    public PotentialCatalogLookupWork AcceptNewRequest(CatalogSearchAttempt request)
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

    private static PotentialCatalogLookupWorkStatus PromoteStatus(
        PotentialCatalogLookupWorkStatus currentStatus,
        RiskBand riskBand)
    {
        if (currentStatus != PotentialCatalogLookupWorkStatus.Pending)
        {
            return currentStatus;
        }

        return riskBand == RiskBand.Blocked
            ? PotentialCatalogLookupWorkStatus.Ignored
            : PotentialCatalogLookupWorkStatus.Pending;
    }
}
