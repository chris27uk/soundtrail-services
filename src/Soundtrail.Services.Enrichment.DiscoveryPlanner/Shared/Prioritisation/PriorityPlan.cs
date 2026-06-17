using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;

public sealed record PriorityPlan(
    ActionType ActionType,
    LookupPriorityBand? Priority)
{
    public bool ShouldSchedule => ActionType == ActionType.Schedule;

    public static PriorityPlan Schedule(LookupPriorityBand priority) => new(ActionType.Schedule, priority);

    public static PriorityPlan Defer() => new(ActionType.Defer, null);

    public static PriorityPlan Ignore() => new(ActionType.Ignore, null);
}
