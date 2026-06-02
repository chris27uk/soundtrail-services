using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.ExistingEligibleRequest
{
    public class StorageTests
    {
        [Fact]
        public async Task Given_An_Existing_Candidate_For_The_Same_MusicCatalogId_When_Handled_Then_One_Candidate_Is_Stored()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingEligibleCandidate("mc_track_1");

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

            env.RankedMusicCandidates.Should().ContainSingle();
        }

        [Fact]
        public async Task
            Given_An_Existing_Candidate_For_The_Same_MusicCatalogId_When_Handled_Then_MusicCatalogId_Is_Preserved()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

            var catalogId = env.RankedMusicCandidates[0].MusicCatalogId.Value;
            catalogId.Should().Be(musicCatalogId);
        }

        [Fact]
        public async Task
            Given_An_Existing_Candidate_For_The_Same_MusicCatalogId_When_Handled_Then_RequestCount_Is_Incremented()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

            env.RankedMusicCandidates[0].RequestCount.Should().Be(3);
        }

        [Fact]
        public async Task
            Given_An_Existing_Candidate_For_The_Same_MusicCatalogId_When_Handled_Then_HighestTrustLevelSeen_Is_Updated()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

            env.RankedMusicCandidates[0].HighestTrustLevelSeen.Should().Be(2);
        }

        [Fact]
        public async Task
            Given_An_Existing_Candidate_For_The_Same_MusicCatalogId_When_Handled_Then_RiskScore_Is_Updated()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

            env.RankedMusicCandidates[0].RiskScore.Should().Be(15);
        }

        [Fact]
        public async Task
            Given_An_Existing_Candidate_With_Higher_Trust_When_Handled_Then_HighestTrustLevelSeen_Is_Preserved()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingCandidate(
                Candidates.ExistingCandidate(
                    MusicCatalogId.From(musicCatalogId),
                    requestCount: 2,
                    highestTrustLevelSeen: 3,
                    riskScore: 5,
                    status: RankedMusicCandidateStatus.Pending,
                    nextEligibleAt: null));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 15));

            env.RankedMusicCandidates[0].HighestTrustLevelSeen.Should().Be(3);
        }

        [Fact]
        public async Task
            Given_An_Existing_Candidate_With_Higher_Risk_When_Handled_Then_RiskScore_Is_Preserved()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingCandidate(
                Candidates.ExistingCandidate(
                    MusicCatalogId.From(musicCatalogId),
                    requestCount: 2,
                    highestTrustLevelSeen: 0,
                    riskScore: 70,
                    status: RankedMusicCandidateStatus.Pending,
                    nextEligibleAt: null));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

            env.RankedMusicCandidates[0].RiskScore.Should().Be(70);
        }

        [Fact]
        public async Task
            Given_An_Existing_Pending_Candidate_When_A_Blocked_Request_Is_Handled_Then_Status_Becomes_Ignored()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 90));

            env.RankedMusicCandidates[0].Status.Should().Be(RankedMusicCandidateStatus.Ignored);
        }

        [Fact]
        public async Task
            Given_An_Existing_Pending_Candidate_When_A_High_Risk_Request_Is_Handled_Then_Status_Remains_Pending()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 60));

            env.RankedMusicCandidates[0].Status.Should().Be(RankedMusicCandidateStatus.Pending);
        }

        [Fact]
        public async Task
            Given_An_Existing_Pending_Candidate_When_A_High_Risk_Request_Is_Handled_Then_RequestCount_Is_Incremented_And_No_Command_Is_Returned()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 60));

            command.Should().BeNull();
            env.RankedMusicCandidates[0].RequestCount.Should().Be(3);
        }
    }
}
