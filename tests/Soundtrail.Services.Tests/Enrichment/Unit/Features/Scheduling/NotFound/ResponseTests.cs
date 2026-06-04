using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Enrichment.Shared.Search.Resolution;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Scheduling.NotFound
{
    public class ResponseTests
    {
        [Fact]
        public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_ResolutionFailedException_Is_Thrown()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.Fails();

            var act = async () => await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 100));

            var exception = await Assert.ThrowsAsync<ResolutionFailedException>(act);
            exception.Outcome.Should().Be(MusicCatalogResolutionOutcome.NotFound);
        }

        [Fact]
        public async Task Given_A_Request_With_A_Weak_Top_Match_When_Handled_Then_ResolutionFailedException_Is_Thrown()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.79m));

            var act = async () => await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 10));

            var exception = await Assert.ThrowsAsync<ResolutionFailedException>(act);
            exception.Outcome.Should().Be(MusicCatalogResolutionOutcome.NotFound);
        }

        [Fact]
        public async Task Given_A_Request_With_Ambiguous_Matches_When_Handled_Then_ResolutionFailedException_Is_Thrown()
        {
            var env = LookupMusicRequestHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.92m),
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.85m));

            var act = async () => await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 10));

            var exception = await Assert.ThrowsAsync<ResolutionFailedException>(act);
            exception.Outcome.Should().Be(MusicCatalogResolutionOutcome.Ambiguous);
        }
    }
}
