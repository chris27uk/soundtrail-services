using FluentAssertions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling.NoPreviousRequests
{
    public class ResponsesTests
    {
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Command_Has_Resolved_MusicCatalogId()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
        }

        [Fact]
        public async Task Given_Multiple_Exact_Identity_Matches_When_Only_One_Matches_The_Local_Release_Date_Then_That_Match_Is_Selected()
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
                    new MusicCatalogMatchEvidence(true, null, null, null, null, "mbid-2", new DateOnly(2005, 6, 7))));

            var result = await env.Handler.Handle(
                env.Request("rare unknown song", trustLevel: 1, riskScore: 10) with
                {
                    Criteria = CatalogSearchCriteria.Track(TrackId.From("mc_track_criteria"))
                });

            result.Command?.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
        }

        [Fact]
        public async Task Given_A_Request_With_A_Top_Match_At_The_Minimum_Accepted_Score_When_Handled_Then_A_Command_Is_Returned()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.80m));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.ShouldSchedule.Should().BeTrue();
        }

        [Fact]
        public async Task Given_A_Request_With_A_Top_Match_At_The_Minimum_Accepted_Score_When_Handled_Then_The_Command_Uses_The_Top_Match_MusicCatalogId()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.80m));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
        }

        [Fact]
        public async Task Given_A_Request_With_A_Winning_Margin_At_The_Minimum_Required_Gap_When_Handled_Then_A_Command_Is_Returned()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.90m),
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.80m));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.ShouldSchedule.Should().BeTrue();
        }

        [Fact]
        public async Task Given_A_Request_With_A_Winning_Margin_At_The_Minimum_Required_Gap_When_Handled_Then_The_Command_Uses_The_Higher_Scoring_MusicCatalogId()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.90m),
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.80m));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Command_CreatedAt_Matches_Request_OccurredAt()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));
            var occurredAt = new DateTimeOffset(2026, 5, 31, 12, 34, 56, TimeSpan.Zero);

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10, occurredAt: occurredAt));

            result.Command?.CreatedAt.Should().Be(occurredAt);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Command_CorrelationId_Is_Populated()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.CorrelationId.Value.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_CommandId_Is_Built_From_The_MusicCatalogId()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.CommandId.Should().Be(CommandId.For("LookupCanonicalMusicMetadata:mc_track_1"));
        }

        [Fact]
        public async Task Given_A_Medium_Risk_Resolved_Request_When_Handled_Then_Command_Is_Returned()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 30));

            result.ShouldSchedule.Should().BeTrue();
        }

        [Fact]
        public async Task Given_A_High_Risk_Resolved_Request_When_Handled_Then_No_Command_Is_Returned()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 60));

            result.ShouldSchedule.Should().BeFalse();
        }

        [Fact]
        public async Task Given_A_Blocked_Risk_Resolved_Request_When_Handled_Then_No_Command_Is_Returned()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 90));

            result.ShouldSchedule.Should().BeFalse();
        }

        [Theory]
        [InlineData(29)]
        [InlineData(30)]
        [InlineData(59)]
        public async Task Given_A_Low_Or_Medium_Risk_Resolved_Request_When_Handled_Then_Command_Is_Returned(int riskScore)
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: riskScore));

            result.ShouldSchedule.Should().BeTrue();
        }

        [Fact]
        public async Task Given_A_Low_Risk_Low_Demand_Request_When_Handled_Then_Command_Has_Low_Priority()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.Priority.Should().Be(LookupPriorityBand.Low);
            result.EstimatedRetryAfterSeconds.Should().Be(30);
            result.Reason.Should().Be("Planner queued lookup");
        }

        [Theory]
        [InlineData(60)]
        [InlineData(89)]
        [InlineData(90)]
        public async Task Given_A_High_Or_Blocked_Risk_Resolved_Request_When_Handled_Then_No_Command_Is_Returned(int riskScore)
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: riskScore));

            result.ShouldSchedule.Should().BeFalse();
            result.EstimatedRetryAfterSeconds.Should().Be(60);
            result.Reason.Should().Be("Planner deferred lookup");
        }
    }
}
