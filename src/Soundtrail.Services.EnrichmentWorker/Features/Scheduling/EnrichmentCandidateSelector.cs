using Soundtrail.Services.EnrichmentWorker.Jobs;
using Soundtrail.Services.EnrichmentWorker.Models;

namespace Soundtrail.Services.EnrichmentWorker.Scheduling;

public sealed class EnrichmentCandidateSelector(EnrichmentPriorityCalculator priorityCalculator, NextStageDecider nextStageDecider)
{
    public IReadOnlyList<EnrichmentCandidate> Select(
        IEnumerable<ResolutionDemand> unresolvedDemand,
        DateTimeOffset now)
    {
        return unresolvedDemand
            .Where(demand => demand.Status is ResolutionDemandStatus.Unresolved or ResolutionDemandStatus.PartiallyResolved)
            .Where(demand => !demand.IsSuspicious)
            .Where(demand => demand.NextEligibleAt is null || demand.NextEligibleAt <= now)
            .Select(demand =>
            {
                var nextStage = nextStageDecider.Decide(demand);
                if (nextStage is null)
                {
                    return null;
                }

                return new EnrichmentCandidate(
                    demand,
                    nextStage.Value,
                    priorityCalculator.Calculate(demand, nextStage.Value));
            })
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderByDescending(candidate => candidate.PriorityScore)
            .ToArray();
    }
}

public sealed record EnrichmentCandidate(
    ResolutionDemand Demand,
    EnrichmentStage Stage,
    int PriorityScore);
