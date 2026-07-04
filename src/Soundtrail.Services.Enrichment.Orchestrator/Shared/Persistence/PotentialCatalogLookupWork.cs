using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;

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

    public static PotentialCatalogLookupWork Create(SearchCatalogRequested requested, MusicCatalogId musicCatalogId)
    {
        var riskBand = ToRiskBand(requested.RiskScore);

        return new PotentialCatalogLookupWork(
            MusicCatalogId: musicCatalogId,
            RequestCount: 1,
            HighestTrustLevelSeen: requested.TrustLevel,
            RiskScore: requested.RiskScore,
            Status: riskBand == RiskBand.Blocked ? PotentialCatalogLookupWorkStatus.Ignored : PotentialCatalogLookupWorkStatus.Pending,
            NextEligibleAt: null);
    }

    public PotentialCatalogLookupWork AcceptNewRequest(SearchCatalogRequested requested)
    {
        var updatedRiskScore = Math.Max(RiskScore, requested.RiskScore);

        return this with
        {
            RequestCount = RequestCount + 1,
            HighestTrustLevelSeen = Math.Max(HighestTrustLevelSeen, requested.TrustLevel),
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
