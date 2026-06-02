using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.ExistingEligibleRequest
{
    public class ResponsesTests
    {
        [Fact]
        public async Task Given_An_Existing_Candidate_For_The_Same_MusicCatalogId_When_Handled_Then_Command_Has_MusicCatalogId()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingEligibleCandidate(musicCatalogId);
            env.Search.ResolveAs(musicCatalogId);

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

            command?.MusicCatalogId.Value.Should().Be(musicCatalogId);
        }

        [Fact]
        public async Task Given_An_Existing_Resolved_Candidate_When_Handled_Then_No_Command_Is_Returned()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingCandidate(
                Candidates.ExistingCandidate(
                    MusicCatalogId.From(musicCatalogId),
                    requestCount: 2,
                    highestTrustLevelSeen: 0,
                    riskScore: 5,
                    status: RankedMusicCandidateStatus.Resolved,
                    nextEligibleAt: null));

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15));

            command.Should().BeNull();
        }

        [Fact]
        public async Task Given_An_Existing_Eligible_Candidate_At_NextEligibleAt_When_Handled_Then_Command_Is_Returned()
        {
            const string musicCatalogId = "mc_track_1";
            var occurredAt = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithExistingCandidate(
                Candidates.ExistingCandidate(
                    MusicCatalogId.From(musicCatalogId),
                    requestCount: 2,
                    highestTrustLevelSeen: 0,
                    riskScore: 5,
                    status: RankedMusicCandidateStatus.Pending,
                    nextEligibleAt: occurredAt));

            var command = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 2, riskScore: 15, occurredAt: occurredAt));

            command.Should().NotBeNull();
        }
    }
}
