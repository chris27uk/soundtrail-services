using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling.NotFound
{
    public class ResponseTests
    {
        [Fact]
        public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_No_Work_Is_Scheduled_And_Discovery_Is_Rejected()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.Fails();

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 100));

            result.ShouldSchedule.Should().BeFalse();
            env.DiscoveryRepository
                .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
                .Last().Should().BeOfType<DiscoveryRejected>();
        }

        [Fact]
        public async Task Given_A_Request_With_A_Weak_Top_Match_When_Handled_Then_No_Work_Is_Scheduled_And_Discovery_Is_Rejected()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.79m));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 10));

            result.ShouldSchedule.Should().BeFalse();
            env.DiscoveryRepository
                .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
                .Last().Should().BeOfType<DiscoveryRejected>();
        }

        [Fact]
        public async Task Given_A_Request_With_Ambiguous_Matches_When_Handled_Then_No_Work_Is_Scheduled_And_Discovery_Is_Rejected()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.92m),
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.85m));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 10));

            result.ShouldSchedule.Should().BeFalse();
            env.DiscoveryRepository
                .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
                .Last().Should().BeOfType<DiscoveryRejected>();
        }

        [Fact]
        public async Task Given_Multiple_Exact_Query_Matches_When_Handled_Then_No_Work_Is_Scheduled_And_Discovery_Is_Rejected()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(
                    MusicCatalogId.From("mc_track_1"),
                    0.91m,
                    new MusicCatalogMatchEvidence(
                        false,
                        "rare unknown song",
                        "test artist",
                        null,
                        null,
                        null,
                        null)),
                new MusicCatalogMatch(
                    MusicCatalogId.From("mc_track_2"),
                    0.90m,
                    new MusicCatalogMatchEvidence(
                        false,
                        "rare unknown song",
                        "test artist",
                        null,
                        null,
                        null,
                        null)));

            var result = await env.Handler.Handle(env.Request("rare unknown song test artist", trustLevel: 0, riskScore: 10));

            result.ShouldSchedule.Should().BeFalse();
            env.DiscoveryRepository
                .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song test artist"))
                .Last().Should().BeOfType<DiscoveryRejected>();
        }

        [Fact]
        public async Task Given_Multiple_Exact_Identity_Matches_With_The_Same_Local_Release_Date_When_Handled_Then_No_Work_Is_Scheduled_And_Discovery_Is_Rejected()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.LocalSearch.Seed(new LocalMusicTrackSearchResult(
                MusicCatalogId.From("mc_track_criteria"),
                "Rare Unknown Song",
                "Test Artist",
                "Rare Album",
                null,
                null,
                null,
                IsPlayable: false,
                ReleaseDate: new DateOnly(2004, 6, 7)));
            env.Search.ReturnMatches(
                new MusicCatalogMatch(
                    MusicCatalogId.From("mc_track_1"),
                    0.99m,
                    new MusicCatalogMatchEvidence(true, null, null, null, null, "mbid-1", new DateOnly(2004, 6, 7))),
                new MusicCatalogMatch(
                    MusicCatalogId.From("mc_track_2"),
                    1.00m,
                    new MusicCatalogMatchEvidence(true, null, null, null, null, "mbid-2", new DateOnly(2004, 6, 7))));

            var result = await env.Handler.Handle(
                env.Request("rare unknown song", trustLevel: 0, riskScore: 10) with
                {
                    Criteria = CatalogSearchCriteria.Track(TrackId.From("mc_track_criteria"))
                });

            result.ShouldSchedule.Should().BeFalse();
            env.DiscoveryRepository
                .GetStoredEvents(CatalogSearchCriteria.Track(TrackId.From("mc_track_criteria")))
                .Last().Should().BeOfType<DiscoveryRejected>();
        }
    }
}
