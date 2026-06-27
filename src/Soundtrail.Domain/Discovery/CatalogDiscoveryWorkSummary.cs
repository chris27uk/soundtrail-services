using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogDiscoveryWorkSummary(
    MusicCatalogId MusicCatalogId,
    int RequestCount,
    int HighestTrustLevelSeen,
    int RiskScore,
    CatalogDiscoveryWorkStatus Status,
    DateTimeOffset? NextEligibleAt,
    LookupPriorityBand? Priority,
    string? Reason)
{
    public bool IsSuspicious => RiskScore >= 60;

    public bool IsPending => Status == CatalogDiscoveryWorkStatus.Pending;

    public bool IsEligibleAt(DateTimeOffset when) => NextEligibleAt is null || NextEligibleAt <= when;
}
