namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Scheduling;

public sealed class DiscoveryBacklogSchedulingOptions
{
    public const string SectionName = "DiscoveryBacklogScheduling";

    public int BatchSize { get; init; } = 25;

    public int RunIntervalSeconds { get; init; } = 60;
}
