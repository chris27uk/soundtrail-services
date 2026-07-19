using Microsoft.Extensions.Options;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Assesment;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;

public sealed class PlanningAssessmentPolicy(IOptions<PlanningAssessmentOptions> options) : IPlanningAssessmentPolicy
{
    private readonly PlanningAssessmentOptions options = options.Value;

    public PlanningAssessment Evaluate(PlanningAssessment assessment)
    {
        var isHighPriority = assessment.Priority == LookupPriorityBand.High;
        var lowPriorityCapacity = Math.Max(0, this.options.MaxConcurrentPlannedWork - this.options.ReservedSlotsForHighPriority);
        return assessment with
        {
            DeferredUntil = assessment.RequestedAt.AddSeconds(this.options.DefaultDeferredSeconds),
            ExpectedCompletionAt = assessment.RequestedAt.AddSeconds(EstimateDurationSeconds(assessment.Target)),
            HighPriorityCapacityIsProtected = !isHighPriority && assessment.Projection.ActiveWorkCount >= lowPriorityCapacity,
            PlannerCapacityIsFull = assessment.Projection.ActiveWorkCount >= this.options.MaxConcurrentPlannedWork
        };
    }

    private int EstimateDurationSeconds(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.SearchForUnknownCatalogItem => this.options.SearchAssessmentSeconds,
            EnrichmentTarget.KnownCatalogItemOperation(var operation) when IsStreamingLocation(operation) => this.options.StreamingLocationSeconds,
            EnrichmentTarget.KnownCatalogItemOperation => this.options.CatalogExpansionSeconds,
            _ => this.options.CatalogExpansionSeconds
        };

    private static bool IsStreamingLocation(CatalogItemOperation operation) =>
        operation is CatalogItemOperation.StreamingLocationForTrack;
}
