using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;

public sealed class DiscoveryPriorityPolicy
{
    public PriorityPlan Investigate(PotentialCatalogLookupWork candidate, DateTimeOffset now)
    {
        if (!candidate.IsPending)
        {
            return PriorityPlan.Ignore(now);
        }

        if (!candidate.IsEligibleAt(now))
        {
            return PriorityPlan.Defer(now);
        }

        if (candidate.IsSuspicious)
        {
            return PriorityPlan.Ignore(now);
        }

        if (candidate.RiskBand == RiskBand.Medium)
        {
            return PriorityPlan.Schedule(LookupPriorityBand.Low);
        }

        if (candidate.HighestTrustLevelSeen >= 2 || candidate.RequestCount >= 2)
        {
            return PriorityPlan.Schedule(LookupPriorityBand.High);
        }

        return PriorityPlan.Schedule(LookupPriorityBand.Low);
    }
}
