using FluentAssertions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling.NoPreviousRequests
{
    public class StorageTests
    {
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_A_Candidate_Is_Stored()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks.Should().ContainSingle();
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_Has_Resolved_MusicCatalogId()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].MusicCatalogId.Value.Should().Be("mc_track_1");
        }
        
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_RequestCount_Is_One()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].RequestCount.Should().Be(1);
        }
        
        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_HighestTrustLevelSeen_Is_Set()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].HighestTrustLevelSeen.Should().Be(1);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_RiskScore_Is_Set()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].RiskScore.Should().Be(10);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_Status_Is_Pending()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].Status.Should().Be(PotentialCatalogLookupWorkStatus.Pending);
        }

        [Fact]
        public async Task Given_A_Blocked_Risk_Resolved_Request_When_Handled_Then_Stored_Candidate_Status_Is_Ignored()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 90));

            env.PotentialCatalogLookupWorks[0].Status.Should().Be(PotentialCatalogLookupWorkStatus.Ignored);
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_Stored_Candidate_NextEligibleAt_Is_Null()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.PotentialCatalogLookupWorks[0].NextEligibleAt.Should().BeNull();
        }

        [Fact]
        public async Task Given_A_Resolved_Request_When_Handled_Then_A_CatalogSearchTracking_Is_Stored()
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10));

            env.CatalogSearchTrackings.Should().ContainSingle();
            env.CatalogSearchTrackings[0].Criteria.Value.Should().Be("search:track:rare unknown song");
            env.CatalogSearchTrackings[0].MusicCatalogId.Value.Should().Be("mc_track_1");
        }

        [Theory]
        [InlineData(60)]
        [InlineData(90)]
        public async Task Given_A_High_Or_Blocked_Risk_Resolved_Request_When_Handled_Then_Stored_Candidate_RiskScore_Is_Persisted(int riskScore)
        {
            var env = CatalogSearchAttemptHandlerTestEnvironment.WithNoExistingCandidates();
            env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

            await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: riskScore));

            env.PotentialCatalogLookupWorks[0].RiskScore.Should().Be(riskScore);
        }
    }
}
