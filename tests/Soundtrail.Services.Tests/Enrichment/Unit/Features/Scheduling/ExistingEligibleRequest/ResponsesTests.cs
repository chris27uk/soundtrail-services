using FluentAssertions;
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
    }
}
