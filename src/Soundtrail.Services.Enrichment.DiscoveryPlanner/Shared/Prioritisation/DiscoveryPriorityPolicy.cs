using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;

public sealed class DiscoveryPriorityPolicy
{
    public PriorityPlan Investigate(RankedMusicCandidate candidate, DateTimeOffset now)
    {
        if (!candidate.IsPending)
        {
            return PriorityPlan.Ignore();
        }

        if (!candidate.IsEligibleAt(now))
        {
            return PriorityPlan.Defer();
        }

        if (candidate.IsSuspicious)
        {
            return PriorityPlan.Ignore();
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
