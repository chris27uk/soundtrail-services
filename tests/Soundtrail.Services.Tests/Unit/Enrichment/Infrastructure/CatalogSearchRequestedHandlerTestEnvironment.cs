using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure
{
    internal sealed class CatalogSearchRequestedHandlerTestEnvironment
    {
        private readonly FakeMusicCatalogCandidateSearch search;
        private readonly LocalMusicTrackSearchFake localMusicTrackSearchFake;
        private readonly CatalogSearchDiscoveryRepositoryFake catalogSearchDiscoveryRepositoryFake;

        private CatalogSearchRequestedHandlerTestEnvironment(
            FakeMusicCatalogCandidateSearch search,
            PotentialCatalogLookupWorkStoreFake _)
        {
            this.search = search;
            this.localMusicTrackSearchFake = new LocalMusicTrackSearchFake();
            this.catalogSearchDiscoveryRepositoryFake = new CatalogSearchDiscoveryRepositoryFake();
            this.Handler = new SearchCatalogRequestedHandler(
                search,
                this.catalogSearchDiscoveryRepositoryFake,
                this.localMusicTrackSearchFake);

            SeedDefaultLocalTrack("mc_track_1");
            SeedDefaultLocalTrack("mc_track_2");
            SeedDefaultLocalTrack("mc_track_3");
            SeedDefaultLocalTrack("mc_track_high");
            SeedDefaultLocalTrack("mc_track_low");
            SeedDefaultLocalTrack("mc_track_deferred");
        }

        public SearchCatalogRequestedHandler Handler { get; }

        public FakeMusicCatalogCandidateSearch Search => this.search;

        public LocalMusicTrackSearchFake LocalSearch => this.localMusicTrackSearchFake;

        public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => this.catalogSearchDiscoveryRepositoryFake;

        public static CatalogSearchRequestedHandlerTestEnvironment WithNoExistingCandidates() =>
            new(
                new FakeMusicCatalogCandidateSearch(),
                new PotentialCatalogLookupWorkStoreFake());

        public static CatalogSearchRequestedHandlerTestEnvironment WithExistingEligibleCandidate(string musicCatalogId = "SomeId")
        {
            var store = new PotentialCatalogLookupWorkStoreFake();
            var search = new FakeMusicCatalogCandidateSearch();
            store.Seed(Candidates.ExistingCandidate(musicCatalogId));
            search.ResolveAs(musicCatalogId);
            return new CatalogSearchRequestedHandlerTestEnvironment(search, store);
        }

        public static CatalogSearchRequestedHandlerTestEnvironment WithExistingNotYetEligibleCandidate(string musicCatalogId = "SomeId")
        {
            var store = new PotentialCatalogLookupWorkStoreFake();
            var search = new FakeMusicCatalogCandidateSearch();
            store.Seed(Candidates.NotYetEligibleCandidate(musicCatalogId));
            search.ResolveAs(musicCatalogId);
            return new CatalogSearchRequestedHandlerTestEnvironment(search, store);
        }

        public static CatalogSearchRequestedHandlerTestEnvironment WithExistingCandidate(PotentialCatalogLookupWork candidate)
        {
            var store = new PotentialCatalogLookupWorkStoreFake();
            var search = new FakeMusicCatalogCandidateSearch();
            store.Seed(candidate);
            search.ResolveAs(candidate.MusicCatalogId);
            return new CatalogSearchRequestedHandlerTestEnvironment(search, store);
        }

        public SearchCatalogRequested Request(
            string query,
            int trustLevel,
            int riskScore,
            DateTimeOffset? occurredAt = null) =>
            new(
                SearchCriteria: MusicSearchCriteria.ByQuery(query, SearchTypesFilter.Tracks),
                Playback: PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
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
