using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;

public sealed class DiscoveryPriorityPolicy : ICatalogDiscoveryWorkPlanningPolicy
{
    public CatalogDiscoveryWorkAssessment Assess(CatalogDiscoveryWorkSummary candidate, DateTimeOffset now)
    {
        if (!candidate.IsPending)
        {
            return new CatalogDiscoveryWorkAssessment(
                CatalogDiscoveryWorkAction.Ignore,
                null,
                60,
                now.AddSeconds(60),
                "Planner deferred lookup");
        }

        if (!candidate.IsEligibleAt(now))
        {
            return new CatalogDiscoveryWorkAssessment(
                CatalogDiscoveryWorkAction.Defer,
                null,
                60,
                now.AddSeconds(60),
                "Planner deferred lookup");
        }

        if (candidate.IsSuspicious)
        {
            return new CatalogDiscoveryWorkAssessment(
                CatalogDiscoveryWorkAction.Defer,
                null,
                60,
                now.AddSeconds(60),
                "Planner deferred lookup");
        }

        if (candidate.RiskScore >= 30)
        {
            return new CatalogDiscoveryWorkAssessment(
                CatalogDiscoveryWorkAction.Schedule,
                LookupPriorityBand.Low,
                30,
                null,
                "Planner queued lookup");
        }

        if (candidate.HighestTrustLevelSeen >= 2 || candidate.RequestCount >= 2)
        {
            return new CatalogDiscoveryWorkAssessment(
                CatalogDiscoveryWorkAction.Schedule,
                LookupPriorityBand.High,
                30,
                null,
                "Planner queued lookup");
        }

        return new CatalogDiscoveryWorkAssessment(
            CatalogDiscoveryWorkAction.Schedule,
            LookupPriorityBand.Low,
            30,
            null,
            "Planner queued lookup");
    }
}
