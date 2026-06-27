using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class CatalogSearchRequestedHandlerTests
{
    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_Only_Streaming_Locations_Required_Is_Appended()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<StreamingLocationsRequired>()
            .Which.MusicCatalogId.Should()
            .Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public async Task Given_A_Query_With_Multiple_High_Scoring_Matches_When_Handled_Then_Each_Match_With_Missing_Providers_Is_Recorded()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ReturnMatches(
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.92m),
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.90m),
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_3"), 0.79m));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks))
            .OfType<StreamingLocationsRequired>()
            .Select(x => x.MusicCatalogId.Value)
            .Should()
            .BeEquivalentTo("mc_track_1", "mc_track_2");
    }

    [Fact]
    public async Task Given_A_Request_With_No_High_Scoring_Matches_When_Handled_Then_Music_Metadata_Required_Is_Appended()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ReturnMatches(new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.79m));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<TrackMetadataLookupRequested>();
    }

}
