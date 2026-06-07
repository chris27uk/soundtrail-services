using FluentAssertions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.NotFound
{
    public class StorageTests
    {
        [Fact]
        public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_No_Candidate_Is_Stored()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.Fails();

            try
            {
                await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 100));
            }
            catch (ResolutionFailedException)
            {
            }

            env.RankedMusicCandidates.Should().BeEmpty();
        }
    }
}
