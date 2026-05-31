using Soundtrail.Services.Enrichment.Features.Scheduling;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.NotFound
{
    public class ResponseTests
    {
        [Fact]
        public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_ResolutionFailedException_Is_Thrown()
        {
            var env = LookupMusicSchedulerHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.Fails();

            var act = async () => await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 100));

            await Assert.ThrowsAsync<ResolutionFailedException>(act);
        }
    }
}
