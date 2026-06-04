using Soundtrail.Services.Enrichment.Shared.Persistence;
using Soundtrail.Services.Enrichment.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.Shared.Prioritisation;

public sealed class DiscoveryPriorityPolicy
{
    public LookupPlan Plan(RankedMusicCandidate candidate, DateTimeOffset now)
    {
        if (!candidate.IsPending)
        {
            return LookupPlan.Ignore();
        }

        if (!candidate.IsEligibleAt(now))
        {
            return LookupPlan.Defer();
        }

        if (candidate.IsSuspicious)
        {
            return LookupPlan.Ignore();
        }

        if (candidate.RiskBand == RiskBand.Medium)
        {
            return LookupPlan.Schedule(LookupPriorityBand.Low);
        }

        if (candidate.HighestTrustLevelSeen >= 2 || candidate.RequestCount >= 2)
        {
            return LookupPlan.Schedule(LookupPriorityBand.High);
        }

        return LookupPlan.Schedule(LookupPriorityBand.Low);
    }
}
