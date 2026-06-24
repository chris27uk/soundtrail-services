using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.Adapters;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using System.Linq;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ApplyEnrichmentResponse;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenMusicCatalogMetadataFetchedFlowResponsesTests
{
    [Fact]
    public async Task Given_Canonical_And_Playback_Responses_When_Applied_Then_The_Track_Becomes_Playable()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();

        using (var seedSession = raven.Store.OpenAsyncSession())
        {
            var trackingStore = new RavenCatalogSearchTrackingStore(raven.Store, seedSession);
            await trackingStore.UpsertAsync(
                new CatalogSearchTracking(
                    CatalogSearchCriteria.Search("track", "rare unknown song"),
                    MusicCatalogId.From("mc_track_1"),
                    new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero)),
                CancellationToken.None);
            await seedSession.SaveChangesAsync();
        }

        using (var canonicalSession = raven.Store.OpenAsyncSession())
        {
            var listener = CreateListener(canonicalSession);
            await listener.Handle(CanonicalAttemptDto(), null!);
            await canonicalSession.SaveChangesAsync();
        }

        using (var playbackSession = raven.Store.OpenAsyncSession())
        {
            var listener = CreateListener(playbackSession);
            await listener.Handle(PlaybackAttemptDto(), null!);
            await playbackSession.SaveChangesAsync();
        }

        await ReplayProjectionsAsync(raven);
        await ReplayDiscoveryLifecycleAsync(raven);

        using var verificationSession = raven.Store.OpenAsyncSession();
        var track = await verificationSession.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId("mc_track_1"),
            CancellationToken.None);

        track.Should().NotBeNull();
        track!.AppleId.Should().Be("apple-track-1");
        track.IsPlayable.Should().BeTrue();
        track.Title.Should().Be("Rare Unknown Song");
        track.Artist.Should().Be("Test Artist");

        var status = await verificationSession.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(CatalogSearchCriteria.Search("track", "rare unknown song").Value),
            CancellationToken.None);
        status.Should().NotBeNull();
        status!.Status.Should().Be("Completed");

        var trackStatus = await verificationSession.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(CatalogSearchCriteria.Track(TrackId.From("mc_track_1")).Value),
            CancellationToken.None);
        var artistStatus = await verificationSession.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(CatalogSearchCriteria.Artist(ArtistId.From("artist_test_artist")).Value),
            CancellationToken.None);
        var albumStatus = await verificationSession.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(CatalogSearchCriteria.Album(AlbumId.From("album_rare_album")).Value),
            CancellationToken.None);

        trackStatus.Should().NotBeNull();
        artistStatus.Should().NotBeNull();
        albumStatus.Should().NotBeNull();
        trackStatus!.Status.Should().Be("Completed");
        artistStatus!.Status.Should().Be("Completed");
        albumStatus!.Status.Should().Be("Completed");
    }

    private static MusicCatalogLookupAttemptedListener CreateListener(Raven.Client.Documents.Session.IAsyncDocumentSession session) =>
        new(new MusicCatalogLookupAttemptedHandler(
            new RavenMusicTrackStreamStore(session),
            new RavenCatalogSearchTrackingStore(session.Advanced.DocumentStore, session),
            new RavenMusicCatalogMetadataFetchedCatalogSearchDiscoveryRepository(session)));

    private static async Task ReplayProjectionsAsync(RavenEmbeddedTestDatabase raven)
    {
        using var querySession = raven.Store.OpenAsyncSession();
        var streamMetadata = await querySession.Advanced.LoadStartingWithAsync<MusicTrackEventStreamMetadataRecordDto>(
            "music-track-streams/");
        var musicCatalogIds = streamMetadata.Select(x => x.MusicCatalogId).Distinct(StringComparer.Ordinal).ToList();

        using var session = raven.Store.OpenAsyncSession();
        var replayHandler = new ReplayMusicTrackHandler(
            new RavenLoadStoredMusicTrackEvents(session),
            new MusicTrackChangedHandler(
                new RavenLoadMusicTrackProjection(session, new RavenMusicTrackProjectionMapper()),
                new RavenSaveMusicTrackProjection(session, new RavenMusicTrackProjectionMapper())));

        foreach (var musicCatalogId in musicCatalogIds)
        {
            await replayHandler.Handle(
                new ReplayMusicTrackCommand(MusicCatalogId.From(musicCatalogId)),
                CancellationToken.None);
        }
    }

    private static async Task ReplayDiscoveryLifecycleAsync(RavenEmbeddedTestDatabase raven)
    {
        using var querySession = raven.Store.OpenAsyncSession();
        var streamMetadata = await querySession.Advanced.LoadStartingWithAsync<DiscoveryQueryEventStreamMetadataRecordDto>(
            "discovery-query-streams/");
        var criteriaValues = streamMetadata.Select(x => x.Criteria).ToList();

        using var session = raven.Store.OpenAsyncSession();
        var replayHandler = new ReplayCatalogSearchStatusHandler(
            new RavenLoadStoredDiscoveryLifecycleEvents(session),
            new CatalogSearchStatusChangedHandler(
                new RavenLoadDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper()),
                new RavenSaveDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper())));

        foreach (var criteria in criteriaValues.Distinct(StringComparer.Ordinal))
        {
            await replayHandler.Handle(
                new ReplayCatalogSearchStatusCommand(CatalogSearchCriteria.From(criteria)),
                CancellationToken.None);
        }
    }

    private static MusicCatalogLookupAttemptedDto CanonicalAttemptDto() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1").Value,
            "mc_track_1",
            ProviderName.MusicBrainz.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            new MusicCatalogLookupOutcomeDto("Completed", null, null, null),
            new MusicCatalogMetadataFetchedDto(
                CommandId.For("ResolveCanonicalMetadata:mc_track_1").Value,
                "mc_track_1",
                ProviderName.MusicBrainz.Value,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
                new SongMetadataDto("Rare Unknown Song", "Test Artist", "isrc-1", "mbid-1", 123000, "Rare Album", new DateOnly(2026, 1, 1)),
                [],
                [],
                "artist_test_artist",
                "album_rare_album",
                "corr-1"));

    private static MusicCatalogLookupAttemptedDto PlaybackAttemptDto() =>
        new(
            CommandId.For("LookupStreamingLocations:mc_track_1").Value,
            "mc_track_1",
            ProviderName.Odesli.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
            "corr-2",
            new MusicCatalogLookupOutcomeDto("Completed", null, null, null),
            new MusicCatalogMetadataFetchedDto(
                CommandId.For("LookupStreamingLocations:mc_track_1").Value,
                "mc_track_1",
                ProviderName.Odesli.Value,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
                null,
                [new ExternalReferenceDto(ProviderName.AppleMusic.Value, new Uri("https://music.apple.com/track/1"), "apple-track-1")],
                [],
                "artist_test_artist",
                "album_rare_album",
                "corr-2"));
}
