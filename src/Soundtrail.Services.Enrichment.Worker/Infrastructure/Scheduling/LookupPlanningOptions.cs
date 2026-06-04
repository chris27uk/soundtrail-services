namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Scheduling;

public sealed class LookupPlanningOptions
{
    public const string SectionName = "LookupPlanning";

    public int SweepBatchSize { get; init; } = 25;

    public int SweepIntervalSeconds { get; init; } = 60;
}
