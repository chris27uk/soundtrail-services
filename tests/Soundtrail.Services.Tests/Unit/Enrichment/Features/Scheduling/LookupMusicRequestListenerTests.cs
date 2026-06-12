using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class LookupMusicRequestListenerTests
{
    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_A_MusicBrainz_Command_Dto_Is_Returned()
    {
        var env = LookupMusicRequestListenerTestEnvironment.WithASchedulableRequest();
        var messages = await env.HandleSchedulableRequest();
        messages.Should().ContainSingle().Which.Should().BeOfType<LookupCanonicalMusicMetadataCommandDto>();
    }

    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_The_CommandId_Is_Built_From_The_MusicCatalogId()
    {
        var env = LookupMusicRequestListenerTestEnvironment.WithASchedulableRequest();
        var message = (LookupCanonicalMusicMetadataCommandDto)(await env.HandleSchedulableRequest()).Single();
        message.CommandId.Should().Be(CommandId.For("LookupCanonicalMusicMetadata:mc_track_1").Value);
    }

    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_The_MusicCatalogId_Is_Preserved()
    {
        var env = LookupMusicRequestListenerTestEnvironment.WithASchedulableRequest();
        var message = (LookupCanonicalMusicMetadataCommandDto)(await env.HandleSchedulableRequest()).Single();
        message.MusicCatalogId.Should().Be("mc_track_1");
    }

    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_The_Priority_Is_Preserved()
    {
        var env = LookupMusicRequestListenerTestEnvironment.WithASchedulableRequest();
        var message = (LookupCanonicalMusicMetadataCommandDto)(await env.HandleSchedulableRequest()).Single();
        message.Priority.Should().Be(LookupPriorityBand.Low);
    }

    [Fact]
    public async Task Given_Local_Search_Has_Isrc_When_Handled_Then_A_Playback_References_Command_Dto_Is_Returned()
    {
        var env = LookupMusicRequestListenerTestEnvironment.WithASchedulableRequest();
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
        var env = LookupMusicRequestListenerTestEnvironment.WithAnUnschedulableRequest();
        var messages = await env.HandleUnschedulableRequest();
        messages.Should().BeEmpty();
    }
}
