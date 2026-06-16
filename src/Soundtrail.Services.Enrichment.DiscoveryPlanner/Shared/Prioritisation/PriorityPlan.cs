using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;

public sealed record PriorityPlan(
    ActionType ActionType,
    LookupPriorityBand? Priority,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason)
{
    public bool ShouldSchedule => ActionType == ActionType.Schedule;

    public static PriorityPlan Schedule(LookupPriorityBand priority) =>
        new(ActionType.Schedule, priority, 30, null, "Planner queued lookup");

    public static PriorityPlan Defer(DateTimeOffset now) =>
        new(ActionType.Defer, null, 60, now.AddSeconds(60), "Planner deferred lookup");

    public static PriorityPlan Ignore(DateTimeOffset now) =>
        new(ActionType.Ignore, null, 60, now.AddSeconds(60), "Planner deferred lookup");
}
