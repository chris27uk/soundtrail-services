using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.NoPreviousRequests
{
    public class StorageTests
    {
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_A_Candidate_Is_Stored()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.RankedMusicCandidates.Should().ContainSingle();
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_Has_Resolved_MusicCatalogId()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.RankedMusicCandidates[0].MusicCatalogId.Value.Should().Be("mc_track_1");
        }
        
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_RequestCount_Is_One()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.RankedMusicCandidates[0].RequestCount.Should().Be(1);
        }
        
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_HighestTrustLevelSeen_Is_Set()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.RankedMusicCandidates[0].HighestTrustLevelSeen.Should().Be(1);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_RiskScore_Is_Set()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.RankedMusicCandidates[0].RiskScore.Should().Be(10);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_Status_Is_Pending()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.RankedMusicCandidates[0].Status.Should().Be(RankedMusicCandidateStatus.Pending);
        }

        [Fact]
        public async Task Given_A_Blocked_Risk_Resolved_Request_When_Handled_Then_Stored_Candidate_Status_Is_Ignored()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 90));

            env.RankedMusicCandidates[0].Status.Should().Be(RankedMusicCandidateStatus.Ignored);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_NextEligibleAt_Is_Null()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.RankedMusicCandidates[0].NextEligibleAt.Should().BeNull();
        }

        [Theory]
        [InlineData(60)]
        [InlineData(90)]
        public async Task Given_A_High_Or_Blocked_Risk_Resolved_Request_When_Handled_Then_Stored_Candidate_RiskScore_Is_Persisted(int riskScore)
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: riskScore));

            env.RankedMusicCandidates[0].RiskScore.Should().Be(riskScore);
        }
    }
}
