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
    }
}
