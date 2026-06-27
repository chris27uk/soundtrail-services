using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class CatalogSearchRequestedListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_A_Search_Start_Event_Is_Stored()
    {
        var env = CatalogSearchRequestedListenerWolverineTestEnvironment.WithASchedulableRequest();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");

        await env.HandleSchedulableRequest();

        env.DiscoveryRepository.GetStoredEvents(criteria)
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<MusicTrackSearchStarted>();
    }

    [Fact]
    public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_No_Discovery_Event_Is_Stored()
    {
        var env = CatalogSearchRequestedListenerWolverineTestEnvironment.WithAnUnschedulableRequest();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");

        await env.HandleUnschedulableRequest();

        env.DiscoveryRepository.GetStoredEvents(criteria).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_Local_Search_Has_Isrc_When_Handled_Then_The_Request_Handler_Still_Only_Stores_Search_Start_Events()
    {
        var env = CatalogSearchRequestedListenerWolverineTestEnvironment.WithASchedulableRequest();
        env.LocalSearch.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_1"),
            "Song A",
            "Artist A",
            "Album A",
            "isrc-1",
            "mbid-1",
            123000,
            IsPlayable: false,
            ReleaseDate: null));

        await env.HandleSchedulableRequest();

        env.DiscoveryRepository
            .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
            .Should()
            .OnlyContain(x => x is MusicTrackSearchStarted);
    }
}
