using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure
{
    public static class Candidates
    {
        public static RankedMusicCandidate ExistingCandidate(
            MusicCatalogId musicCatalogId,
            int requestCount = 2,
            int highestTrustLevelSeen = 0,
            int riskScore = 5,
            RankedMusicCandidateStatus status= RankedMusicCandidateStatus.Pending,
            DateTimeOffset? nextEligibleAt = null)
        {
            return new RankedMusicCandidate(
                MusicCatalogId: musicCatalogId,
                RequestCount: requestCount,
                HighestTrustLevelSeen: highestTrustLevelSeen,
                RiskScore: riskScore,
                Status: status,
                NextEligibleAt: nextEligibleAt);
        }
        
        public static RankedMusicCandidate NotYetEligibleCandidate(MusicCatalogId musicCatalogId)
        {
            return new RankedMusicCandidate(
                MusicCatalogId: musicCatalogId,
                RequestCount: 1,
                HighestTrustLevelSeen: 0,
                RiskScore: 0,
                Status: RankedMusicCandidateStatus.Pending,
                NextEligibleAt: new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero));
        }
    }
}
