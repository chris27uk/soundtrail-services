using Soundtrail.Services.Enrichment.Features.Scheduling;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure
{
    internal sealed class LookupMusicSchedulerHandlerTestEnvironment
    {
        private readonly FakeMusicCatalogSearch search;
        private readonly RankedMusicCandidateStoreFake rankedMusicCandidateStoreFake;

        private LookupMusicSchedulerHandlerTestEnvironment(
            FakeMusicCatalogSearch search,
            RankedMusicCandidateStoreFake rankedMusicCandidateStoreFake)
        {
            this.search = search;
            this.rankedMusicCandidateStoreFake = rankedMusicCandidateStoreFake;
            this.Handler = new LookupSchedulerHandler(search, rankedMusicCandidateStoreFake);
        }

        public LookupSchedulerHandler Handler { get; }

        public FakeMusicCatalogSearch Search => this.search;

        public IReadOnlyList<RankedMusicCandidate> RankedMusicCandidates => this.rankedMusicCandidateStoreFake.All;

        public static LookupMusicSchedulerHandlerTestEnvironment WithNoExistingCandidates() =>
            new(
                new FakeMusicCatalogSearch(),
                new RankedMusicCandidateStoreFake());

        public static LookupMusicSchedulerHandlerTestEnvironment WithExistingEligibleCandidate(string musicCatalogId = "SomeId")
        {
            var store = new RankedMusicCandidateStoreFake();
            var search = new FakeMusicCatalogSearch();
            store.Seed(Candidates.ExistingCandidate(musicCatalogId));
            search.ResolveAs(musicCatalogId);
            return new LookupMusicSchedulerHandlerTestEnvironment(search, store);
        }
        
        public static LookupMusicSchedulerHandlerTestEnvironment WithExistingNotYetEligibleCandidate(string musicCatalogId = "SomeId")
        {
            var store = new RankedMusicCandidateStoreFake();
            var search = new FakeMusicCatalogSearch();
            store.Seed(Candidates.NotYetEligibleCandidate(musicCatalogId));
            search.ResolveAs(musicCatalogId);
            return new LookupMusicSchedulerHandlerTestEnvironment(search, store);
        }

        public LookupMusicRequest Request(
            string query,
            int trustLevel,
            int riskScore,
            DateTimeOffset? occurredAt = null) =>
            new(
                Query: NormalizedSearchQuery.FromText(query),
                TrustLevel: trustLevel,
                RiskScore: riskScore,
                OccurredAt: occurredAt ?? new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
                CorrelationId: "correlation-1");
    }
}
