namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Hosting;

public sealed class SchedulerOptions
{
    public const string SectionName = "DiscoveryBacklogScheduling";

    public int BatchSize { get; init; } = 25;

    public int RunIntervalSeconds { get; init; } = 60;
}
