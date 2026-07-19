namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;

public sealed class PlanningAssessmentOptions
{
    public const string SectionName = "PlanningAssessment";

    public int MaxConcurrentPlannedWork { get; init; } = 24;

    public int ReservedSlotsForHighPriority { get; init; } = 8;

    public int DefaultDeferredSeconds { get; init; } = 30;

    public int SearchAssessmentSeconds { get; init; } = 45;

    public int CatalogExpansionSeconds { get; init; } = 20;

    public int StreamingLocationSeconds { get; init; } = 10;
}
