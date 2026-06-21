using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class CatalogSearchAttemptListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_A_MusicBrainz_Command_Dto_Is_Returned()
    {
        var env = CatalogSearchAttemptListenerWolverineTestEnvironment.WithASchedulableRequest();
        var messages = await env.HandleSchedulableRequest();
        messages.Should().ContainSingle().Which.Should().BeOfType<LookupCanonicalMusicMetadataCommandDto>();
    }

    [Fact]
    public async Task Given_Local_Search_Has_Isrc_When_Handled_Then_A_Playback_References_Command_Dto_Is_Returned()
    {
        var env = CatalogSearchAttemptListenerWolverineTestEnvironment.WithASchedulableRequest();
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

        var message = (ResolvePlaybackReferencesCommandDto)(await env.HandleSchedulableRequest()).Single();

        message.SearchTerm.Isrc.Should().Be("isrc-1");
    }

    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_Discovery_Is_Planned_And_Started()
    {
        var env = CatalogSearchAttemptListenerWolverineTestEnvironment.WithASchedulableRequest();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");

        await env.HandleSchedulableRequest();

        var events = env.DiscoveryRepository.GetStoredEvents(criteria);
        var planned = events.OfType<DiscoveryPlanned>().Single();
        var started = events.OfType<DiscoveryStarted>().Single();

        planned.WillBeLookedUp.Should().BeTrue();
        planned.EstimatedRetryAfterSeconds.Should().Be(30);
        planned.Reason.Should().Be("Planner queued lookup");
        started.Reason.Should().Be("Lookup started");
    }

    [Fact]
    public async Task Given_A_Deferred_Request_When_Handled_Then_Discovery_Status_Is_Projected_As_Deferred()
    {
        var env = CatalogSearchAttemptListenerWolverineTestEnvironment.WithADeferredRequest();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");

        var messages = await env.HandleDeferredRequest();

        messages.Should().BeEmpty();
        var @event = env.DiscoveryRepository.GetStoredEvents(criteria).Single().Should().BeOfType<DiscoveryDeferred>().Subject;
        @event.WillBeLookedUp.Should().BeTrue();
        @event.EstimatedRetryAfterSeconds.Should().Be(60);
        @event.Reason.Should().Be("Planner deferred lookup");
    }

    [Fact]
    public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_Discovery_Status_Is_Projected_As_Rejected()
    {
        var env = CatalogSearchAttemptListenerWolverineTestEnvironment.WithAnUnschedulableRequest();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");

        var messages = await env.HandleUnschedulableRequest();

        messages.Should().BeEmpty();
        var @event = env.DiscoveryRepository.GetStoredEvents(criteria).Single().Should().BeOfType<DiscoveryRejected>().Subject;
        @event.WillBeLookedUp.Should().BeFalse();
        @event.Reason.Should().Be("Planner rejected lookup");
    }
}
