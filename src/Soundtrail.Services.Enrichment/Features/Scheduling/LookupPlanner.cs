using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling;

public sealed class LookupPlanner
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
