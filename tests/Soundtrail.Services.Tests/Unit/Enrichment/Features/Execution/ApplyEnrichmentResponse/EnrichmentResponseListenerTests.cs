using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.ApplyEnrichmentResponse;

public sealed class EnrichmentResponseListenerTests
{
    [Fact]
    public async Task Given_A_MusicBrainz_Response_Dto_When_Handled_Then_A_Stream_Is_Created()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAMusicBrainzResponseDto();
        await env.HandleMusicBrainzResponse();
        env.StreamStore.Streams.Should().ContainKey("mc_track_1");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_Dto_When_Handled_Then_A_MusicBrainz_Snapshot_Is_Saved()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAMusicBrainzResponseDto();
        await env.HandleMusicBrainzResponse();
        env.SnapshotStore.Snapshots.Should().ContainKey("mc_track_1:MusicBrainz");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_Dto_When_Handled_Then_A_TrackDiscovered_Fact_Is_Stored()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAMusicBrainzResponseDto();
        await env.HandleMusicBrainzResponse();
        env.StreamStore.Streams["mc_track_1"].Events.Should().ContainItemsAssignableTo<TrackDiscovered>();
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_Dto_When_Handled_Then_Artist_And_Album_Discovered_Facts_Are_Stored_From_Response_Hierarchy()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAMusicBrainzResponseDto();

        await env.HandleMusicBrainzResponse();

        env.StreamStore.Streams["mc_track_1"].Events.OfType<ArtistDiscovered>().Should().ContainSingle();
        env.StreamStore.Streams["mc_track_1"].Events.OfType<AlbumDiscovered>().Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Duplicate_Response_Dto_When_Handled_Then_Only_A_Single_CommandId_Is_Recorded()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithADuplicateMusicBrainzResponseDto();
        await env.HandleDuplicateMusicBrainzResponse();
        env.StreamStore.Streams["mc_track_1"].AppliedCommandIds.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_Dto_When_Handled_Then_Discovery_Status_Is_Projected_As_Completed()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAMusicBrainzResponseDto();

        await env.HandleMusicBrainzResponse();

        var status = env.DiscoveryStatus.Updates["search:track:rare unknown song"];
        status.Status.Should().Be(CatalogSearchLifecycleStatus.Completed);
        status.WillBeLookedUp.Should().BeFalse();
    }

    [Fact]
    public async Task Given_Multiple_Search_Trackings_For_The_Same_MusicCatalogId_When_Handled_Then_All_Tracked_Searches_Are_Projected_As_Completed()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithMultipleTrackingsForTheSameMusicCatalogId();

        await env.HandleMusicBrainzResponse();

        env.DiscoveryStatus.Updates.Keys.Should().BeEquivalentTo(
            "search:track:rare unknown song",
            "search:track:rare unknown song live");
    }

    [Fact]
    public async Task Given_A_Playback_References_Response_With_Failed_Providers_When_Handled_Then_ProviderReferenceLookupFailed_Facts_Are_Stored()
    {
        var env = EnrichmentResponseListenerTestEnvironment.WithAMusicBrainzResponseDto();

        await env.Listener.Handle(
            new EnrichmentResponseDto(
                CommandId.For("ResolvePlaybackReferences:mc_track_1").Value,
                "mc_track_1",
                ProviderName.Odesli.Value,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
                null,
                [],
                [
                    new ProviderLookupFailureDto(ProviderName.Spotify.Value, ProviderName.Odesli.Value),
                    new ProviderLookupFailureDto(ProviderName.YoutubeMusic.Value, ProviderName.Odesli.Value)
                ],
                null,
                null,
                "corr-2"),
            null!);

        env.StreamStore.Streams["mc_track_1"].Events.OfType<ProviderReferenceLookupFailed>()
            .Select(x => x.Provider)
            .Should().BeEquivalentTo(new[] { ProviderName.Spotify, ProviderName.YoutubeMusic });
    }
}
