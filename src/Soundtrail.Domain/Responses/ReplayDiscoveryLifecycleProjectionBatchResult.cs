namespace Soundtrail.Domain.Responses;

public sealed record ReplayDiscoveryLifecycleProjectionBatchResult(
    int ReplayedCriteriaCount,
    int ReplayedEventCount);
