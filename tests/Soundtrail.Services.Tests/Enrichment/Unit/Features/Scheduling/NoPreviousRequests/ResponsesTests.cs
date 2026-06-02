using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.NoPreviousRequests
{
    public class ResponsesTests
    {
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Command_Has_Resolved_MusicCatalogId()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            command?.MusicCatalogId.Value.Should().Be("mc_track_1");
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Command_CreatedAt_Matches_Request_OccurredAt()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));
            var occurredAt = new DateTimeOffset(2026, 5, 31, 12, 34, 56, TimeSpan.Zero);

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10, occurredAt: occurredAt));

            command?.CreatedAt.Should().Be(occurredAt);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Command_CorrelationId_Matches_Request()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            command?.CorrelationId.Should().Be("correlation-1");
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_CommandId_Is_Populated()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            command?.CommandId.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Given_A_Medium_Risk_Resolved_Request_When_Handled_Then_Command_Is_Returned()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 30));

            command.Should().NotBeNull();
        }

        [Fact]
        public async Task Given_A_High_Risk_Resolved_Request_When_Handled_Then_No_Command_Is_Returned()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 60));

            command.Should().BeNull();
        }

        [Fact]
        public async Task Given_A_Blocked_Risk_Resolved_Request_When_Handled_Then_No_Command_Is_Returned()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 90));

            command.Should().BeNull();
        }

        [Theory]
        [InlineData(29)]
        [InlineData(30)]
        [InlineData(59)]
        public async Task Given_A_Low_Or_Medium_Risk_Resolved_Request_When_Handled_Then_Command_Is_Returned(int riskScore)
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: riskScore));

            command.Should().NotBeNull();
        }

        [Theory]
        [InlineData(60)]
        [InlineData(89)]
        [InlineData(90)]
        public async Task Given_A_High_Or_Blocked_Risk_Resolved_Request_When_Handled_Then_No_Command_Is_Returned(int riskScore)
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: riskScore));

            command.Should().BeNull();
        }
    }
}
