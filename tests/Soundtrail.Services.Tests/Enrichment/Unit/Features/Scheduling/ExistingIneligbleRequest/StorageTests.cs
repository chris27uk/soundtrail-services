using FluentAssertions;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.ExistingIneligbleRequest
{
    public class StorageTests
    {
        [Fact]
        public async Task Given_A_Resolved_Request_That_Is_Not_Yet_Eligible_When_Handled_Then_A_Candidate_Is_Stored()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicRequestHandlerTestEnvironment.WithExistingNotYetEligibleCandidate(musicCatalogId);

            await env.Handler.Handle(env.Request(
                "rare unknown song",
                trustLevel: 1,
                riskScore: 0,
                occurredAt: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero)));

            env.RankedMusicCandidates.Should().ContainSingle();
        }
        
        [Fact]
        public async Task Given_A_Resolved_Request_That_Is_Not_Yet_Eligible_When_Handled_Then_RequestCount_Is_Incremented()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicRequestHandlerTestEnvironment.WithExistingNotYetEligibleCandidate(musicCatalogId);

            await env.Handler.Handle(env.Request(
                "rare unknown song",
                trustLevel: 1,
                riskScore: 0,
                occurredAt: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero)));

            env.RankedMusicCandidates[0].RequestCount.Should().Be(2);
        }
    }
}
