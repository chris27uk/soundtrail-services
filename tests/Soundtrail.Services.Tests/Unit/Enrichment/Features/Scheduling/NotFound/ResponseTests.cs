using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling.NotFound
{
    public class ResponseTests
    {
        [Fact]
        public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_No_Work_Is_Scheduled_And_Discovery_Is_Rejected()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.Fails();

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 100));

            result.ShouldSchedule.Should().BeFalse();
            env.DiscoveryRepository
                .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
                .Last().Should().BeOfType<DiscoveryRejected>();
        }

        [Fact]
        public async Task Given_A_Request_With_A_Weak_Top_Match_When_Handled_Then_No_Work_Is_Scheduled_And_Discovery_Is_Rejected()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.79m));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 10));

            result.ShouldSchedule.Should().BeFalse();
            env.DiscoveryRepository
                .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
                .Last().Should().BeOfType<DiscoveryRejected>();
        }

        [Fact]
        public async Task Given_A_Request_With_Ambiguous_Matches_When_Handled_Then_No_Work_Is_Scheduled_And_Discovery_Is_Rejected()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ReturnMatches(
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.92m),
                new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.85m));

            var result = await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 10));

            result.ShouldSchedule.Should().BeFalse();
            env.DiscoveryRepository
                .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
                .Last().Should().BeOfType<DiscoveryRejected>();
        }
    }
}
