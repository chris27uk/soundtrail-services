using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class CatalogSearchAttemptListenerTests
{
    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_A_MusicBrainz_Command_Dto_Is_Returned()
    {
        var env = CatalogSearchAttemptListenerTestEnvironment.WithASchedulableRequest();
        var messages = await env.HandleSchedulableRequest();
        messages.Should().ContainSingle().Which.Should().BeOfType<LookupCanonicalMusicMetadataCommandDto>();
    }

    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_The_CommandId_Is_Built_From_The_MusicCatalogId()
    {
        var env = CatalogSearchAttemptListenerTestEnvironment.WithASchedulableRequest();
        var message = (LookupCanonicalMusicMetadataCommandDto)(await env.HandleSchedulableRequest()).Single();
        message.CommandId.Should().Be(CommandId.For("LookupCanonicalMusicMetadata:mc_track_1").Value);
    }

    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_The_MusicCatalogId_Is_Preserved()
    {
        var env = CatalogSearchAttemptListenerTestEnvironment.WithASchedulableRequest();
        var message = (LookupCanonicalMusicMetadataCommandDto)(await env.HandleSchedulableRequest()).Single();
        message.MusicCatalogId.Should().Be("mc_track_1");
    }

    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_The_Priority_Is_Preserved()
    {
        var env = CatalogSearchAttemptListenerTestEnvironment.WithASchedulableRequest();
        var message = (LookupCanonicalMusicMetadataCommandDto)(await env.HandleSchedulableRequest()).Single();
        message.Priority.Should().Be(LookupPriorityBand.Low);
    }

    [Fact]
    public async Task Given_Local_Search_Has_Isrc_When_Handled_Then_A_Playback_References_Command_Dto_Is_Returned()
    {
        var env = CatalogSearchAttemptListenerTestEnvironment.WithASchedulableRequest();
        env.LocalSearch.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_1"),
            "Song A",
            "Artist A",
            "Album A",
            "isrc-1",
            "mbid-1",
            123000,
            IsPlayable: false));

        var message = (ResolvePlaybackReferencesCommandDto)(await env.HandleSchedulableRequest()).Single();
        
        message.SearchTerm.Isrc.Should().Be("isrc-1");
    }

    [Fact]
    public async Task Given_An_Unschedulable_Request_When_Handled_Then_No_Messages_Are_Returned()
    {
        var env = CatalogSearchAttemptListenerTestEnvironment.WithAnUnschedulableRequest();
        var messages = await env.HandleUnschedulableRequest();
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_Discovery_Status_Is_Projected_As_Planned()
    {
        var env = CatalogSearchAttemptListenerTestEnvironment.WithASchedulableRequest();

        await env.HandleSchedulableRequest();

        var status = env.DiscoveryStatus.Updates[CatalogSearchCriteria.Search("track", "rare unknown song").Value];
        status.Status.Should().Be(CatalogSearchLifecycleStatus.Planned);
        status.WillBeLookedUp.Should().BeTrue();
        status.EstimatedRetryAfterSeconds.Should().Be(30);
        status.Reason.Should().Be("Planner queued lookup");
    }

    [Fact]
    public async Task Given_A_Deferred_Request_When_Handled_Then_Discovery_Status_Is_Projected_As_Deferred()
    {
        var env = CatalogSearchAttemptListenerTestEnvironment.WithADeferredRequest();

        var messages = await env.HandleDeferredRequest();

        messages.Should().BeEmpty();
        var status = env.DiscoveryStatus.Updates[CatalogSearchCriteria.Search("track", "rare unknown song").Value];
        status.Status.Should().Be(CatalogSearchLifecycleStatus.Deferred);
        status.WillBeLookedUp.Should().BeTrue();
        status.EstimatedRetryAfterSeconds.Should().Be(60);
        status.Reason.Should().Be("Planner deferred lookup");
    }

    [Fact]
    public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_Discovery_Status_Is_Projected_As_Rejected()
    {
        var env = CatalogSearchAttemptListenerTestEnvironment.WithAnUnschedulableRequest();

        var messages = await env.HandleUnschedulableRequest();

        messages.Should().BeEmpty();
        var status = env.DiscoveryStatus.Updates[CatalogSearchCriteria.Search("track", "rare unknown song").Value];
        status.Status.Should().Be(CatalogSearchLifecycleStatus.Rejected);
        status.WillBeLookedUp.Should().BeFalse();
        status.Reason.Should().Be("Planner rejected lookup");
    }
}
