using FluentAssertions;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.ExistingEligibleRequest
{
    public class ResponsesTests
    {
        [Fact]
        public async Task Given_An_Existing_Candidate_For_The_Same_MusicCatalogId_When_Handled_Then_Command_Has_MusicCatalogId()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicRequestHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);
            env.Search.ResolveAs(musicCatalogId);

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

            result.Command?.MusicCatalogId.Should().Be(MusicCatalogId.From(musicCatalogId));
        }

        [Fact]
        public async Task Given_An_Existing_Popular_Candidate_When_Handled_Then_Command_Has_High_Priority()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicRequestHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.Priority.Should().Be(LookupPriorityBand.High);
        }

        [Fact]
        public async Task Given_An_Existing_High_Trust_Candidate_When_Handled_Then_Command_Has_High_Priority()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicRequestHandlerTestEnvironment.WithExistingCandidate(
                Candidates.ExistingCandidate(
                    MusicCatalogId.From(musicCatalogId),
                    requestCount: 1,
                    highestTrustLevelSeen: 2,
                    riskScore: 5,
                    status: RankedMusicCandidateStatus.Pending,
                    nextEligibleAt: null));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            result.Command?.Priority.Should().Be(LookupPriorityBand.High);
        }

        [Fact]
        public async Task Given_An_Existing_Resolved_Candidate_When_Handled_Then_No_Command_Is_Returned()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicRequestHandlerTestEnvironment.WithExistingCandidate(
                Candidates.ExistingCandidate(
                    MusicCatalogId.From(musicCatalogId),
                    requestCount: 2,
                    highestTrustLevelSeen: 0,
                    riskScore: 5,
                    status: RankedMusicCandidateStatus.Resolved,
                    nextEligibleAt: null));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

            result.ShouldSchedule.Should().BeFalse();
        }

        [Fact]
        public async Task Given_An_Existing_Ignored_Candidate_When_Handled_Then_No_Command_Is_Returned()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicRequestHandlerTestEnvironment.WithExistingCandidate(
                Candidates.ExistingCandidate(
                    MusicCatalogId.From(musicCatalogId),
                    requestCount: 2,
                    highestTrustLevelSeen: 0,
                    riskScore: 90,
                    status: RankedMusicCandidateStatus.Ignored,
                    nextEligibleAt: null));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 90));

            result.ShouldSchedule.Should().BeFalse();
        }

        [Fact]
        public async Task Given_An_Existing_Eligible_Candidate_At_NextEligibleAt_When_Handled_Then_Command_Is_Returned()
        {
            const string musicCatalogId = "mc_track_1";
            var occurredAt = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
            var env = LookupMusicRequestHandlerTestEnvironment.WithExistingCandidate(
                Candidates.ExistingCandidate(
                    MusicCatalogId.From(musicCatalogId),
                    requestCount: 2,
                    highestTrustLevelSeen: 0,
                    riskScore: 5,
                    status: RankedMusicCandidateStatus.Pending,
                    nextEligibleAt: occurredAt));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15, occurredAt: occurredAt));

            result.ShouldSchedule.Should().BeTrue();
        }
    }
}
