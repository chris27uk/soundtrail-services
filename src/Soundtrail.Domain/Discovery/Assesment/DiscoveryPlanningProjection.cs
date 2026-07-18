namespace Soundtrail.Domain.Discovery.Assesment;

public sealed record DiscoveryPlanningProjection(
    bool HasEquivalentWorkInFlight,
    DateTimeOffset? EquivalentWorkExpectedCompletionAt,
    int ActiveWorkCount,
    int ActiveHighPriorityWorkCount);
