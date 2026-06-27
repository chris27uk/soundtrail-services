using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Translators.ProjectionDocuments;
using Soundtrail.Translators.MusicTrackEventStore;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ApplyEnrichmentResponse;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenMusicCatalogMetadataFetchedFlowResponsesTests
{
    private static readonly IMusicTrackStoredEventRecordTranslator Translator = MusicTrackStoredEventRecordTranslator.Default;
    [Fact]
    public async Task Given_Resolved_Metadata_And_Playback_Responses_When_Applied_Then_The_Track_Becomes_Playable()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();

        using (var seedSession = raven.Store.OpenAsyncSession())
        {
            var trackingStore = new RavenCatalogSearchTrackingStore(raven.Store, seedSession);
            await trackingStore.UpsertAsync(
                new CatalogSearchTracking(
                    MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks),
                    MusicCatalogId.From("mc_track_1"),
                    new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero)),
                CancellationToken.None);
            await seedSession.SaveChangesAsync();
        }

        using (var metadataSession = raven.Store.OpenAsyncSession())
        {
            var listener = CreateListener(metadataSession);
            await listener.Handle(ResolvedMetadataAttemptDto(), null!);
            await metadataSession.SaveChangesAsync();
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

        var querySearchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var status = await verificationSession.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(querySearchTerm)),
            CancellationToken.None);
        status.Should().NotBeNull();
        status!.Status.Should().Be("Completed");
    }

    private static MusicCatalogLookupAttemptedListener CreateListener(IAsyncDocumentSession session) =>
        new(new MusicCatalogLookupAttemptedHandler(
            new RavenMusicTrackStreamStore(session, Translator),
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
            new RavenLoadStoredMusicTrackEvents(session, Translator),
            new MusicTrackChangedHandler(
                new RavenLoadMusicTrackProjection(session, new RavenMusicTrackProjectionMapper()),
                new RavenSaveMusicTrackProjection(session, Soundtrail.Translators.Registry.TypeTranslationRegistry.Default)));

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
                new RavenSaveDiscoveryLifecycleProjection(session, Soundtrail.Translators.Registry.TypeTranslationRegistry.Default)));

        foreach (var criteria in criteriaValues.Distinct(StringComparer.Ordinal))
        {
            await replayHandler.Handle(
                new ReplayCatalogSearchStatusCommand(MusicSearchTermPersistentIdTranslator.ToDomainObject(criteria)),
                CancellationToken.None);
        }
    }

    private static MusicCatalogLookupAttemptedDto ResolvedMetadataAttemptDto() =>
        new(
            CommandId.For("ResolveMusicMetadata:mc_track_1").Value,
            "mc_track_1",
            LookupSource.MusicBrainz.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            "corr-1",
            new MusicCatalogLookupOutcomeDto("Completed", null, null, null),
            new MusicCatalogMetadataFetchedDto(
                CommandId.For("ResolveMusicMetadata:mc_track_1").Value,
                "mc_track_1",
                LookupSource.MusicBrainz.Value,
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
            LookupSource.Odesli.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
            "corr-2",
            new MusicCatalogLookupOutcomeDto("Completed", null, null, null),
            new MusicCatalogMetadataFetchedDto(
                CommandId.For("LookupStreamingLocations:mc_track_1").Value,
                "mc_track_1",
                LookupSource.Odesli.Value,
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
                null,
                [new ExternalReferenceDto(ProviderName.AppleMusic.Value, new Uri("https://music.apple.com/track/1"), "apple-track-1")],
                [],
                "artist_test_artist",
                "album_rare_album",
                "corr-2"));
}
