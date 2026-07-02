using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;

public sealed class DiscoveryPriorityPolicy : IDiscoveryAssessmentPolicy
{
    public DiscoveryAssessment Assess(DiscoveryAssessmentSummary candidate, DateTimeOffset now)
    {
        if (candidate.IsDeferred && !candidate.IsEligibleAt(now))
        {
            return new DiscoveryAssessment(
                DiscoveryAssessmentAction.Defer,
                null,
                60,
                now.AddSeconds(60),
                "Planner deferred lookup");
        }

        if (candidate.IsSuspicious)
        {
            return new DiscoveryAssessment(
                DiscoveryAssessmentAction.Defer,
                null,
                60,
                now.AddSeconds(60),
                "Planner deferred lookup");
        }

        if (candidate.RiskScore >= 30)
        {
            return new DiscoveryAssessment(
                DiscoveryAssessmentAction.Schedule,
                LookupPriorityBand.Low,
                30,
                null,
                "Planner queued lookup");
        }

        if (candidate.TrustLevel >= 2)
        {
            return new DiscoveryAssessment(
                DiscoveryAssessmentAction.Schedule,
                LookupPriorityBand.High,
                30,
                null,
                "Planner queued lookup");
        }

        return new DiscoveryAssessment(
            DiscoveryAssessmentAction.Schedule,
            LookupPriorityBand.Low,
            30,
            null,
            "Planner queued lookup");
    }
}
