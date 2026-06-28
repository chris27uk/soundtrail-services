using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogDiscoveryWorkSummarySnapshot(
    MusicCatalogId MusicCatalogId,
    int RequestCount,
    int HighestTrustLevelSeen,
    int RiskScore,
    CatalogDiscoveryWorkStatus Status,
    DateTimeOffset? NextEligibleAt,
    LookupPriorityBand? Priority,
    string? Reason,
    DateTimeOffset UpdatedAt,
    int LastAppliedVersion);
