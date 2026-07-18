using Microsoft.Extensions.Options;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicAssessmentRequired.Planning;

public sealed class PlanningAssessmentPolicy(IOptions<PlanningAssessmentOptions> options) : IPlanningAssessmentPolicy
{
    private readonly PlanningAssessmentOptions options = options.Value;

    public PlanningAssessment Evaluate(PlanningAssessment assessment)
    {
        var isHighPriority = assessment.Priority == LookupPriorityBand.High;
        var lowPriorityCapacity = Math.Max(0, options.MaxConcurrentPlannedWork - options.ReservedSlotsForHighPriority);
        return assessment with
        {
            DeferredUntil = assessment.RequestedAt.AddSeconds(options.DefaultDeferredSeconds),
            ExpectedCompletionAt = assessment.RequestedAt.AddSeconds(EstimateDurationSeconds(assessment.Target)),
            HighPriorityCapacityIsProtected = !isHighPriority && assessment.Projection.ActiveWorkCount >= lowPriorityCapacity,
            PlannerCapacityIsFull = assessment.Projection.ActiveWorkCount >= options.MaxConcurrentPlannedWork
        };
    }

    private int EstimateDurationSeconds(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.SearchForUnknownCatalogItem => options.SearchAssessmentSeconds,
            EnrichmentTarget.KnownCatalogItemOperation(var operation) when IsStreamingLocation(operation) => options.StreamingLocationSeconds,
            EnrichmentTarget.KnownCatalogItemOperation => options.CatalogExpansionSeconds,
            _ => options.CatalogExpansionSeconds
        };

    private static bool IsStreamingLocation(CatalogItemOperation operation) =>
        operation is CatalogItemOperation.StreamingLocationForTrack;
}
