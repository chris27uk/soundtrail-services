using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters.Documents;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters.Indexes;

internal sealed class RankedMusicCandidates_ByPlanning : AbstractIndexCreationTask<RavenRankedMusicCandidateDocument>
{
    public RankedMusicCandidates_ByPlanning()
    {
        Map = candidates => from candidate in candidates
                            select new
                            {
                                candidate.Status,
                                candidate.NextEligibleAt,
                                candidate.HighestTrustLevelSeen,
                                candidate.RequestCount,
                                candidate.RiskScore
                            };
    }
}
