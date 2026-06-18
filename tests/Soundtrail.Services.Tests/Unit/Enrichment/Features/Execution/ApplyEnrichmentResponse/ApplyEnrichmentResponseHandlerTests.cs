using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.ApplyEnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandlerTests
{
    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_A_Stream_Is_Created()
    {
        var env = ApplyEnrichmentResponseHandlerTestEnvironment.WithAMusicBrainzResponse();
        await env.HandleMusicBrainzResponse();
        env.StreamStore.Streams.Should().ContainKey("mc_track_1");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_A_MusicBrainz_Snapshot_Is_Saved()
    {
        var env = ApplyEnrichmentResponseHandlerTestEnvironment.WithAMusicBrainzResponse();
        await env.HandleMusicBrainzResponse();
        env.SnapshotStore.Snapshots.Should().ContainKey("mc_track_1:MusicBrainz");
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_A_TrackDiscovered_Fact_Is_Stored()
    {
        var env = ApplyEnrichmentResponseHandlerTestEnvironment.WithAMusicBrainzResponse();
        await env.HandleMusicBrainzResponse();
        env.StreamStore.Streams["mc_track_1"].Events.Should().ContainItemsAssignableTo<TrackDiscovered>();
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_Artist_And_Album_Discovered_Facts_Are_Stored_From_Response_Hierarchy()
    {
        var env = ApplyEnrichmentResponseHandlerTestEnvironment.WithAMusicBrainzResponse();

        await env.HandleMusicBrainzResponse();

        env.StreamStore.Streams["mc_track_1"].Events.OfType<ArtistDiscovered>().Should().ContainSingle();
        env.StreamStore.Streams["mc_track_1"].Events.OfType<AlbumDiscovered>().Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Duplicate_Response_When_Handled_Then_Only_A_Single_CommandId_Is_Recorded()
    {
        var env = ApplyEnrichmentResponseHandlerTestEnvironment.WithADuplicateMusicBrainzResponse();
        await env.HandleDuplicateMusicBrainzResponse();
        env.StreamStore.Streams["mc_track_1"].AppliedCommandIds.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_Discovery_Status_Is_Projected_As_Completed()
    {
        var env = ApplyEnrichmentResponseHandlerTestEnvironment.WithAMusicBrainzResponse();

        await env.HandleMusicBrainzResponse();

        env.DiscoveryRepository
            .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
            .Last().Should().BeOfType<DiscoveryCompleted>();
    }

    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Handled_Then_Entity_Criteria_Are_Also_Projected_As_Completed()
    {
        var env = ApplyEnrichmentResponseHandlerTestEnvironment.WithAMusicBrainzResponse();

        await env.HandleMusicBrainzResponse();

        env.DiscoveryRepository.GetStoredEvents(CatalogSearchCriteria.Track(TrackId.From("mc_track_1"))).Should().NotBeEmpty();
        env.DiscoveryRepository.GetStoredEvents(CatalogSearchCriteria.Artist(ArtistId.From("artist_test_artist"))).Should().NotBeEmpty();
        env.DiscoveryRepository.GetStoredEvents(CatalogSearchCriteria.Album(AlbumId.From("album_rare_album"))).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Given_Multiple_Search_Trackings_For_The_Same_MusicCatalogId_When_Handled_Then_All_Tracked_Searches_Are_Projected_As_Completed()
    {
        var env = ApplyEnrichmentResponseHandlerTestEnvironment.WithMultipleTrackingsForTheSameMusicCatalogId();

        await env.HandleMusicBrainzResponse();

        env.DiscoveryRepository.GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song")).Should().NotBeEmpty();
        env.DiscoveryRepository.GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song live")).Should().NotBeEmpty();
        env.DiscoveryRepository.GetStoredEvents(CatalogSearchCriteria.Track(TrackId.From("mc_track_1"))).Should().NotBeEmpty();
        env.DiscoveryRepository.GetStoredEvents(CatalogSearchCriteria.Artist(ArtistId.From("artist_test_artist"))).Should().NotBeEmpty();
        env.DiscoveryRepository.GetStoredEvents(CatalogSearchCriteria.Album(AlbumId.From("album_rare_album"))).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Given_A_Playback_References_Response_With_Failed_Providers_When_Handled_Then_ProviderReferenceLookupFailed_Facts_Are_Stored()
    {
        var env = ApplyEnrichmentResponseHandlerTestEnvironment.WithAMusicBrainzResponse();

        await env.Handle(
            new EnrichmentResponse(
                CommandId.For("ResolvePlaybackReferences:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.Odesli,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
                null,
                [],
                [
                    new ProviderLookupFailure(ProviderName.Spotify, ProviderName.Odesli),
                    new ProviderLookupFailure(ProviderName.YoutubeMusic, ProviderName.Odesli)
                ],
                null,
                CorrelationId.From("corr-2")));

        env.StreamStore.Streams["mc_track_1"].Events.OfType<ProviderReferenceLookupFailed>()
            .Select(x => x.Provider)
            .Should().BeEquivalentTo(new[] { ProviderName.Spotify, ProviderName.YoutubeMusic });
    }
}
