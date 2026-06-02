namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record LookupPlan(
    LookupPlanningDisposition Disposition,
    LookupPriorityBand? Priority)
{
    public bool ShouldSchedule => Disposition == LookupPlanningDisposition.Schedule;

    public static LookupPlan Schedule(LookupPriorityBand priority) => new(LookupPlanningDisposition.Schedule, priority);

    public static LookupPlan Defer() => new(LookupPlanningDisposition.Defer, null);

    public static LookupPlan Ignore() => new(LookupPlanningDisposition.Ignore, null);
}
