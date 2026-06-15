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
    DateTimeOffset? NextEligibleAt,
    IReadOnlyList<DiscoveryQueryKey> QueryKeys)
{
    public RiskBand RiskBand => ToRiskBand(RiskScore);

    public bool IsSuspicious => RiskBand is RiskBand.High or RiskBand.Blocked;

    public bool IsPending => Status == PotentialCatalogLookupWorkStatus.Pending;

    public bool IsEligibleAt(DateTimeOffset when) => NextEligibleAt is null || NextEligibleAt <= when;

    public static PotentialCatalogLookupWork Create(LookupMusicRequest request, MusicCatalogId musicCatalogId)
    {
        var riskBand = ToRiskBand(request.RiskScore);

        return new PotentialCatalogLookupWork(
            MusicCatalogId: musicCatalogId,
            RequestCount: 1,
            HighestTrustLevelSeen: request.TrustLevel,
            RiskScore: request.RiskScore,
            Status: riskBand == RiskBand.Blocked ? PotentialCatalogLookupWorkStatus.Ignored : PotentialCatalogLookupWorkStatus.Pending,
            NextEligibleAt: null,
            QueryKeys: [request.QueryKey]);
    }

    public PotentialCatalogLookupWork AcceptNewRequest(LookupMusicRequest request)
    {
        var updatedRiskScore = Math.Max(RiskScore, request.RiskScore);

        return this with
        {
            RequestCount = RequestCount + 1,
            HighestTrustLevelSeen = Math.Max(HighestTrustLevelSeen, request.TrustLevel),
            RiskScore = updatedRiskScore,
            Status = PromoteStatus(Status, ToRiskBand(updatedRiskScore)),
            QueryKeys = AddQueryKey(QueryKeys, request.QueryKey)
        };
    }

    private static IReadOnlyList<DiscoveryQueryKey> AddQueryKey(
        IReadOnlyList<DiscoveryQueryKey> existing,
        DiscoveryQueryKey queryKey)
    {
        if (existing.Contains(queryKey))
        {
            return existing;
        }

        return existing.Concat([queryKey]).ToArray();
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
