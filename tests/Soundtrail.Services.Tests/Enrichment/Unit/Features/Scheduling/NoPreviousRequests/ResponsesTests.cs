using FluentAssertions;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.NoPreviousRequests
{
    public class ResponsesTests
    {
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Command_Has_Resolved_MusicCatalogId()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.MusicCatalogId.Value.Should().Be("mc_track_1");
        }

        [Fact]
        public async Task Given_A_Request_With_A_Top_Match_At_The_Minimum_Accepted_Score_When_Handled_Then_A_Command_Is_Returned()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.80m));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.ShouldSchedule.Should().BeTrue();
            result.Command?.MusicCatalogId.Value.Should().Be("mc_track_1");
        }

        [Fact]
        public async Task Given_A_Request_With_A_Winning_Margin_At_The_Minimum_Required_Gap_When_Handled_Then_A_Command_Is_Returned()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.90m),
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.80m));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.ShouldSchedule.Should().BeTrue();
            result.Command?.MusicCatalogId.Value.Should().Be("mc_track_1");
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Command_CreatedAt_Matches_Request_OccurredAt()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));
            var occurredAt = new DateTimeOffset(2026, 5, 31, 12, 34, 56, TimeSpan.Zero);

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10, occurredAt: occurredAt));

            result.Command?.CreatedAt.Should().Be(occurredAt);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Command_CorrelationId_Is_Populated()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.CorrelationId.Value.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_CommandId_Is_Built_From_The_MusicCatalogId()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.CommandId.Should().Be(CommandId.For("mc_track_1"));
        }

        [Fact]
        public async Task Given_A_Medium_Risk_Resolved_Request_When_Handled_Then_Command_Is_Returned()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 30));

            result.ShouldSchedule.Should().BeTrue();
            result.Command?.Priority.Should().Be(LookupPriorityBand.Low);
        }

        [Fact]
        public async Task Given_A_High_Risk_Resolved_Request_When_Handled_Then_No_Command_Is_Returned()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 60));

            result.ShouldSchedule.Should().BeFalse();
        }

        [Fact]
        public async Task Given_A_Blocked_Risk_Resolved_Request_When_Handled_Then_No_Command_Is_Returned()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
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
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: riskScore));

            result.ShouldSchedule.Should().BeTrue();
        }

        [Fact]
        public async Task Given_A_Low_Risk_Low_Demand_Request_When_Handled_Then_Command_Has_Low_Priority()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.Priority.Should().Be(LookupPriorityBand.Low);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(89)]
        [InlineData(90)]
        public async Task Given_A_High_Or_Blocked_Risk_Resolved_Request_When_Handled_Then_No_Command_Is_Returned(int riskScore)
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: riskScore));

            result.ShouldSchedule.Should().BeFalse();
        }
    }
}
