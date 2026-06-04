using FluentAssertions;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.ExistingIneligbleRequest
{
    public class ResponseTests
    {
        [Fact]
        public async Task Given_A_Resolved_Request_That_Is_Not_Yet_Eligible_When_Handled_Then_No_Command_Is_Queued()
        {
            const string musicCatalogId = "mc_track_1";
            var env = LookupMusicRequestHandlerTestEnvironment.WithExistingNotYetEligibleCandidate(musicCatalogId);

            var result = await env.Handler.Handle(env.Request(
                "rare unknown song",
                trustLevel: 1,
                riskScore: 0,
                occurredAt: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero)));

            result.ShouldSchedule.Should().BeFalse();
        }
    }
}
