using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure
{
    internal sealed class LookupMusicRequestHandlerTestEnvironment
    {
        private readonly FakeMusicCatalogCandidateSearch search;
        private readonly RankedMusicCandidateStoreFake rankedMusicCandidateStoreFake;
        private readonly ActiveLookupWorkStoreFake activeLookupWorkStoreFake;

        private LookupMusicRequestHandlerTestEnvironment(
            FakeMusicCatalogCandidateSearch search,
            RankedMusicCandidateStoreFake rankedMusicCandidateStoreFake)
        {
            this.search = search;
            this.rankedMusicCandidateStoreFake = rankedMusicCandidateStoreFake;
            this.activeLookupWorkStoreFake = new ActiveLookupWorkStoreFake();
            this.Planner = new DiscoveryPriorityPolicy();
            this.ResolutionPolicy = new MusicCatalogResolutionPolicy();
            this.Handler = new LookupMusicRequestHandler(
                search,
                rankedMusicCandidateStoreFake,
                this.Planner,
                this.ResolutionPolicy,
                this.activeLookupWorkStoreFake);
        }

        public LookupMusicRequestHandler Handler { get; }

        public DiscoveryPriorityPolicy Planner { get; }

        public MusicCatalogResolutionPolicy ResolutionPolicy { get; }

        public FakeMusicCatalogCandidateSearch Search => this.search;

        public ActiveLookupWorkStoreFake ActiveWorkStore => this.activeLookupWorkStoreFake;

        public IReadOnlyList<RankedMusicCandidate> RankedMusicCandidates => this.rankedMusicCandidateStoreFake.All;

        public static LookupMusicRequestHandlerTestEnvironment WithNoExistingCandidates() =>
            new(
                new FakeMusicCatalogCandidateSearch(),
                new RankedMusicCandidateStoreFake());

        public static LookupMusicRequestHandlerTestEnvironment WithExistingEligibleCandidate(string musicCatalogId = "SomeId")
        {
            var store = new RankedMusicCandidateStoreFake();
            var search = new FakeMusicCatalogCandidateSearch();
            store.Seed(Candidates.ExistingCandidate(musicCatalogId));
            search.ResolveAs(musicCatalogId);
            return new LookupMusicRequestHandlerTestEnvironment(search, store);
        }
        
        public static LookupMusicRequestHandlerTestEnvironment WithExistingNotYetEligibleCandidate(string musicCatalogId = "SomeId")
        {
            var store = new RankedMusicCandidateStoreFake();
            var search = new FakeMusicCatalogCandidateSearch();
            store.Seed(Candidates.NotYetEligibleCandidate(musicCatalogId));
            search.ResolveAs(musicCatalogId);
            return new LookupMusicRequestHandlerTestEnvironment(search, store);
        }

        public static LookupMusicRequestHandlerTestEnvironment WithExistingCandidate(RankedMusicCandidate candidate)
        {
            var store = new RankedMusicCandidateStoreFake();
            var search = new FakeMusicCatalogCandidateSearch();
            store.Seed(candidate);
            search.ResolveAs(candidate.MusicCatalogId);
            return new LookupMusicRequestHandlerTestEnvironment(search, store);
        }

        public Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.LookupMusicRequest Request(
            string query,
            int trustLevel,
            int riskScore,
            DateTimeOffset? occurredAt = null) =>
            new(
                Query: Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.NormalizedSearchQuery.FromText(query),
                TrustLevel: trustLevel,
                RiskScore: riskScore,
                OccurredAt: occurredAt ?? new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
                CorrelationId: CorrelationId.New());
    }
}
