using FluentAssertions;
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
    }
}
