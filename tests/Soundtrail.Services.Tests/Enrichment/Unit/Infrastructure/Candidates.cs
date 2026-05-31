using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure
{
    public static class Candidates
    {
        public static RankedMusicCandidate ExistingCandidate(MusicCatalogId musicCatalogId)
        {
            return new RankedMusicCandidate(
                MusicCatalogId: musicCatalogId,
                RequestCount: 2,
                HighestTrustLevelSeen: 0,
                RiskScore: 5,
                Status: RankedMusicCandidateStatus.Pending,
                NextEligibleAt: null);
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
