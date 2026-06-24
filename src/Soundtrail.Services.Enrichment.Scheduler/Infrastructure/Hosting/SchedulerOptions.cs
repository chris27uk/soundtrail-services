namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Hosting;

public sealed class SchedulerOptions
{
    public const string SectionName = "DiscoveryBacklogScheduling";

    public int RunIntervalSeconds { get; init; } = 60;
}
