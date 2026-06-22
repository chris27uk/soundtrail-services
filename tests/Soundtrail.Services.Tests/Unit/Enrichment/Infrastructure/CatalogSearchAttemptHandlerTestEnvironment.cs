using Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search.Resolution;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure
{
    internal sealed class CatalogSearchAttemptHandlerTestEnvironment
    {
        private readonly FakeMusicCatalogCandidateSearch search;
        private readonly PotentialCatalogLookupWorkStoreFake potentialCatalogLookupWorkStoreFake;
        private readonly ActiveLookupWorkStoreFake activeLookupWorkStoreFake;
        private readonly LocalMusicTrackSearchFake localMusicTrackSearchFake;
        private readonly CatalogSearchTrackingStoreFake catalogSearchTrackingStoreFake;
        private readonly CatalogSearchDiscoveryRepositoryFake catalogSearchDiscoveryRepositoryFake;
        private readonly SourceApiBudgetPortFake sourceApiBudgetPortFake;

        private CatalogSearchAttemptHandlerTestEnvironment(
            FakeMusicCatalogCandidateSearch search,
            PotentialCatalogLookupWorkStoreFake potentialCatalogLookupWorkStoreFake)
        {
            this.search = search;
            this.potentialCatalogLookupWorkStoreFake = potentialCatalogLookupWorkStoreFake;
            this.activeLookupWorkStoreFake = new ActiveLookupWorkStoreFake();
            this.localMusicTrackSearchFake = new LocalMusicTrackSearchFake();
            this.catalogSearchTrackingStoreFake = new CatalogSearchTrackingStoreFake();
            this.catalogSearchDiscoveryRepositoryFake = new CatalogSearchDiscoveryRepositoryFake();
            this.sourceApiBudgetPortFake = new SourceApiBudgetPortFake();
            this.Planner = new DiscoveryPriorityPolicy();
            this.ResolutionPolicy = new MusicCatalogMatchResolver();
            this.Handler = new CatalogSearchAttemptHandler(
                search,
                potentialCatalogLookupWorkStoreFake,
                this.catalogSearchTrackingStoreFake,
                this.catalogSearchDiscoveryRepositoryFake,
                this.Planner,
                this.sourceApiBudgetPortFake,
                this.ResolutionPolicy,
                this.activeLookupWorkStoreFake,
                this.localMusicTrackSearchFake);

            SeedDefaultLocalTrack("mc_track_1");
            SeedDefaultLocalTrack("mc_track_2");
            SeedDefaultLocalTrack("mc_track_3");
            SeedDefaultLocalTrack("mc_track_high");
            SeedDefaultLocalTrack("mc_track_low");
            SeedDefaultLocalTrack("mc_track_deferred");
        }

        public CatalogSearchAttemptHandler Handler { get; }

        public DiscoveryPriorityPolicy Planner { get; }

        public MusicCatalogMatchResolver ResolutionPolicy { get; }

        public FakeMusicCatalogCandidateSearch Search => this.search;

        public ActiveLookupWorkStoreFake ActiveWorkStore => this.activeLookupWorkStoreFake;

        public LocalMusicTrackSearchFake LocalSearch => this.localMusicTrackSearchFake;

        public SourceApiBudgetPortFake SourceBudget => this.sourceApiBudgetPortFake;

        public IReadOnlyList<PotentialCatalogLookupWork> PotentialCatalogLookupWorks => this.potentialCatalogLookupWorkStoreFake.All;

        public IReadOnlyList<CatalogSearchTracking> CatalogSearchTrackings => this.catalogSearchTrackingStoreFake.All;

        public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => this.catalogSearchDiscoveryRepositoryFake;

        public static CatalogSearchAttemptHandlerTestEnvironment WithNoExistingCandidates() =>
            new(
                new FakeMusicCatalogCandidateSearch(),
                new PotentialCatalogLookupWorkStoreFake());

        public static CatalogSearchAttemptHandlerTestEnvironment WithExistingEligibleCandidate(string musicCatalogId = "SomeId")
        {
            var store = new PotentialCatalogLookupWorkStoreFake();
            var search = new FakeMusicCatalogCandidateSearch();
            store.Seed(Candidates.ExistingCandidate(musicCatalogId));
            search.ResolveAs(musicCatalogId);
            return new CatalogSearchAttemptHandlerTestEnvironment(search, store);
        }
        
        public static CatalogSearchAttemptHandlerTestEnvironment WithExistingNotYetEligibleCandidate(string musicCatalogId = "SomeId")
        {
            var store = new PotentialCatalogLookupWorkStoreFake();
            var search = new FakeMusicCatalogCandidateSearch();
            store.Seed(Candidates.NotYetEligibleCandidate(musicCatalogId));
            search.ResolveAs(musicCatalogId);
            return new CatalogSearchAttemptHandlerTestEnvironment(search, store);
        }

        public static CatalogSearchAttemptHandlerTestEnvironment WithExistingCandidate(PotentialCatalogLookupWork candidate)
        {
            var store = new PotentialCatalogLookupWorkStoreFake();
            var search = new FakeMusicCatalogCandidateSearch();
            store.Seed(candidate);
            search.ResolveAs(candidate.MusicCatalogId);
            return new CatalogSearchAttemptHandlerTestEnvironment(search, store);
        }

        public CatalogSearchAttempt Request(
            string query,
            int trustLevel,
            int riskScore,
            DateTimeOffset? occurredAt = null) =>
            new(
                Criteria: CatalogSearchCriteria.Search("track", NormalizedSearchQuery.FromText(query).Value),
                Query: NormalizedSearchQuery.FromText(query),
                TrustLevel: trustLevel,
                RiskScore: riskScore,
                OccurredAt: occurredAt ?? new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
                CorrelationId: CorrelationId.New());

        private void SeedDefaultLocalTrack(string musicCatalogId) =>
            this.localMusicTrackSearchFake.Seed(new LocalMusicTrackSearchResult(
                MusicCatalogId.From(musicCatalogId),
                $"Track {musicCatalogId}",
                $"Artist {musicCatalogId}",
                $"Album {musicCatalogId}",
                null,
                null,
                null,
                IsPlayable: false,
                ReleaseDate: null));
    }
}
