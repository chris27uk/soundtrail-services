using FluentAssertions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling.NoPreviousRequests
{
    public class StorageTests
    {
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_A_Candidate_Is_Stored()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks.Should().ContainSingle();
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_Has_Resolved_MusicCatalogId()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].MusicCatalogId.Value.Should().Be("mc_track_1");
        }
        
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_RequestCount_Is_One()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].RequestCount.Should().Be(1);
        }
        
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_HighestTrustLevelSeen_Is_Set()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].HighestTrustLevelSeen.Should().Be(1);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_RiskScore_Is_Set()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].RiskScore.Should().Be(10);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_Status_Is_Pending()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].Status.Should().Be(PotentialCatalogLookupWorkStatus.Pending);
        }

        [Fact]
        public async Task Given_A_Blocked_Risk_Resolved_Request_When_Handled_Then_Stored_Candidate_Status_Is_Ignored()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 90));

            env.PotentialCatalogLookupWorks[0].Status.Should().Be(PotentialCatalogLookupWorkStatus.Ignored);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_NextEligibleAt_Is_Null()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].NextEligibleAt.Should().BeNull();
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_A_CatalogSearchTracking_Is_Stored()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.CatalogSearchTrackings.Select(x => x.Criteria.Value).Should().BeEquivalentTo(
                "search:track:rare unknown song",
                "track:mc_track_1");
            env.CatalogSearchTrackings.Should().OnlyContain(x => x.MusicCatalogId.Value == "mc_track_1");
        }

        [Fact]
        public async Task Given_A_Resolved_Request_With_Known_Hierarchy_When_Handled_Then_Artist_And_Album_Trackings_Are_Stored()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));
            env.LocalSearch.Seed(new LocalMusicTrackSearchResult(
                MusicCatalogId.From("mc_track_1"),
                "Song A",
                "Artist A",
                "Album A",
                null,
                null,
                null,
                IsPlayable: false,
                ArtistId.From("artist_a"),
                AlbumId.From("album_a")));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.CatalogSearchTrackings.Select(x => x.Criteria.Value).Should().BeEquivalentTo(
                "search:track:rare unknown song",
                "track:mc_track_1",
                "artist:artist_a",
                "album:album_a");
        }

        [Theory]
        [InlineData(60)]
        [InlineData(90)]
        public async Task Given_A_High_Or_Blocked_Risk_Resolved_Request_When_Handled_Then_Stored_Candidate_RiskScore_Is_Persisted(int riskScore)
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: riskScore));

            env.PotentialCatalogLookupWorks[0].RiskScore.Should().Be(riskScore);
        }
    }
}
