using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class CatalogSearchRequestedListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_A_Music_Track_Search_Started_Event_Is_Stored()
    {
        var env = CatalogSearchRequestedListenerWolverineTestEnvironment.WithASchedulableRequest();
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);

        await env.HandleSchedulableRequest();

        env.DiscoveryRepository.GetStoredEvents(criteria)
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<MusicTrackSearchStarted>();
    }

    [Fact]
    public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_A_Music_Metadata_Required_Event_Is_Stored()
    {
        var env = CatalogSearchRequestedListenerWolverineTestEnvironment.WithAnUnschedulableRequest();
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);

        await env.HandleUnschedulableRequest();

        env.DiscoveryRepository.GetStoredEvents(criteria).Should().ContainSingle().Which.Should().BeOfType<TrackMetadataLookupRequested>();
    }

    [Fact]
    public async Task Given_Local_Search_Has_Isrc_When_Handled_Then_The_Request_Handler_Still_Stores_A_Candidate_Event()
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
            AvailableProviders: [],
            ReleaseDate: null));

        await env.HandleSchedulableRequest();

        env.DiscoveryRepository
            .GetStoredEvents(MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks))
            .Should()
            .OnlyContain(x => x is MusicTrackSearchStarted);
    }
}
