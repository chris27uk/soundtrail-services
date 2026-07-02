using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record DiscoveryAssessmentSummary(
    MusicCatalogId MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    CatalogSearchLifecycleStatus Status,
    DateTimeOffset? EarliestExpectedCompletionAt)
{
    public bool IsSuspicious => RiskScore >= 60;

    public bool IsDeferred => Status == CatalogSearchLifecycleStatus.Deferred;

    public bool IsEligibleAt(DateTimeOffset when) =>
        EarliestExpectedCompletionAt is null || EarliestExpectedCompletionAt <= when;
}
