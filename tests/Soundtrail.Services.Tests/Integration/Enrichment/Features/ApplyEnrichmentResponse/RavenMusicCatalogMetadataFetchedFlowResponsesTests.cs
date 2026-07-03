using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Enrichment;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogItemLookupAttempted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogItemLookupAttempted.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.Support;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Adapters.ProjectionDocuments;
using Soundtrail.Services.Tests.Support;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ApplyEnrichmentResponse;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenMusicCatalogMetadataFetchedFlowResponsesTests
{
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
            await ApplyAttemptAsync(metadataSession, ResolvedMetadataAttemptDto());
            await metadataSession.SaveChangesAsync();
        }

        using (var playbackSession = raven.Store.OpenAsyncSession())
        {
            await ApplyAttemptAsync(playbackSession, PlaybackAttemptDto());
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
            CatalogSearchStatusRecordDto.GetDocumentId(DiscoveryQueryKey.StableValueFor(querySearchTerm)),
            CancellationToken.None);
        status.Should().NotBeNull();
        status!.Status.Should().Be("Completed");
    }

    private static async Task ApplyAttemptAsync(IAsyncDocumentSession session, CatalogItemLookupAttemptedDto dto)
    {
        var lookupListener = new CatalogItemLookupAttemptedListener(
            new CatalogItemLookupAttemptedHandler(
                new MusicCatalogLookupAttemptedHandler(TestEventStreamRepositories.CreateMusicCatalogLookup(session)),
                null!,
                null!));

        await lookupListener.Handle(dto, session, CancellationToken.None);

        var lookupId = MusicCatalogLookupId.From(MusicCatalogId.From(dto.ItemValue));
        var historyRepository = TestEventStreamRepositories.CreateMusicCatalogLookup(session);
        var loaded = await historyRepository.LoadAsync(lookupId, CancellationToken.None);
        var command = new MusicCatalogLookupHistoryChangedCommand(
            lookupId,
            loaded.Events.Select((@event, index) => (index + 1, @event)).ToArray());

        await new ApplyMusicCatalogLookupHistoryChangedToCatalogHandler(
            historyRepository,
            TestEventStreamRepositories.CreateArtistCatalog(session)).Handle(command, CancellationToken.None);
        await new ApplyMusicCatalogLookupHistoryChangedToKnownTrackDiscoveryHandler(
            TestEventStreamRepositories.CreateDiscoveryQuery(session)).Handle(command, CancellationToken.None);
        await new ApplyMusicCatalogLookupHistoryChangedToSearchDiscoveryHandler(
            new RavenCatalogSearchTrackingStore(session.Advanced.DocumentStore, session),
            TestEventStreamRepositories.CreateDiscoveryQuery(session)).Handle(command, CancellationToken.None);
    }

    private static async Task ReplayProjectionsAsync(RavenEmbeddedTestDatabase raven)
    {
        using var querySession = raven.Store.OpenAsyncSession();
        var streamMetadata = await querySession.Advanced.LoadStartingWithAsync<RavenEventStreamMetadataRecord>(
            "artist-catalog-streams/");
        var artistIds = streamMetadata.Select(x => x.StreamId).Distinct(StringComparer.Ordinal).ToList();

        using var session = raven.Store.OpenAsyncSession();
        var replayHandler = new MusicCatalogChangedHandler(
            TestEventStreamRepositories.CreateArtistCatalog(session),
            new Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters.RavenSaveMusicTrackCatalogProjection(session));

        foreach (var artistId in artistIds)
        {
            var repository = TestEventStreamRepositories.CreateArtistCatalog(session);
            var loaded = await repository.LoadAsync(ArtistId.From(artistId), CancellationToken.None);
            await replayHandler.Handle(
                new MusicCatalogChangedCommand(
                    ArtistId.From(artistId),
                    loaded.Events.Select((@event, index) => new Soundtrail.Domain.Catalog.Projection.VersionedCatalogEvent(index + 1, @event)).ToArray()),
                CancellationToken.None);
        }
    }

    private static async Task ReplayDiscoveryLifecycleAsync(RavenEmbeddedTestDatabase raven)
    {
        using var querySession = raven.Store.OpenAsyncSession();
        var streamMetadata = await querySession.Advanced.LoadStartingWithAsync<RavenEventStreamMetadataRecord>(
            "discovery-query-streams/");
        var criteriaValues = streamMetadata.Select(x => x.StreamId).ToList();

        using var session = raven.Store.OpenAsyncSession();
        var replayHandler = new ReplayCatalogSearchStatusHandler(
            new RavenLoadStoredDiscoveryLifecycleEvents(session, TypeTranslationRegistry.Default),
            new CatalogSearchStatusChangedHandler(
                new RavenLoadDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper()),
                new RavenSaveDiscoveryLifecycleProjection(session, Soundtrail.Adapters.Registry.TypeTranslationRegistry.Default)));

        foreach (var criteria in criteriaValues.Distinct(StringComparer.Ordinal))
        {
            await replayHandler.Handle(
                new ReplayCatalogSearchStatusCommand(DiscoveryQueryKey.ToMusicSearchCriteria(criteria)),
                CancellationToken.None);
        }
    }

    private static CatalogItemLookupAttemptedDto ResolvedMetadataAttemptDto() =>
        new(
            CommandId.For("ResolveMusicMetadata:mc_track_1").Value,
            CatalogItemKind.Track,
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
                "corr-1"),
            null,
            null);

    private static CatalogItemLookupAttemptedDto PlaybackAttemptDto() =>
        new(
            CommandId.For("LookupStreamingLocations:mc_track_1").Value,
            CatalogItemKind.Track,
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
                "corr-2"),
            null,
            null);
}
