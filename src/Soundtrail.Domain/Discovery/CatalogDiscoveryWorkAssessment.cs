using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogDiscoveryWorkAssessment(
    CatalogDiscoveryWorkAction Action,
    LookupPriorityBand? Priority,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason)
{
    public bool ShouldSchedule => Action == CatalogDiscoveryWorkAction.Schedule;
}
