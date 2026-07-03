using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class MusicCatalogLookupAttemptedHandlerTestEnvironment
{
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;
    private readonly CatalogSearchTrackingStoreFake trackingStore;
    private readonly MusicCatalogLookupHistoryRepositoryFake lookupHistoryRepository;

    private MusicCatalogLookupAttemptedHandlerTestEnvironment()
    {
        Now = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
        StreamStore = new ArtistCatalogEventRepositoryFake();
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        trackingStore = new CatalogSearchTrackingStoreFake();
        lookupHistoryRepository = new MusicCatalogLookupHistoryRepositoryFake();
        trackingStore.Seed(new CatalogSearchTracking(
            MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks),
            MusicCatalogId.From("mc_track_1"),
            Now));

        SeedDiscovery(MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks));

        Handler = new MusicCatalogLookupAttemptedHandler(lookupHistoryRepository);
        CatalogHandler = new ApplyMusicCatalogLookupHistoryChangedToCatalogHandler(
            lookupHistoryRepository,
            StreamStore);
        KnownTrackDiscoveryHandler = new ApplyMusicCatalogLookupHistoryChangedToKnownTrackDiscoveryHandler(
            discoveryRepository);
        SearchDiscoveryHandler = new ApplyMusicCatalogLookupHistoryChangedToSearchDiscoveryHandler(
            trackingStore,
            discoveryRepository);
    }

    public MusicCatalogLookupAttemptedHandler Handler { get; }

    public ApplyMusicCatalogLookupHistoryChangedToCatalogHandler CatalogHandler { get; }

    public ApplyMusicCatalogLookupHistoryChangedToKnownTrackDiscoveryHandler KnownTrackDiscoveryHandler { get; }

    public ApplyMusicCatalogLookupHistoryChangedToSearchDiscoveryHandler SearchDiscoveryHandler { get; }

    public ArtistCatalogEventRepositoryFake StreamStore { get; }

    public CatalogSearchTrackingStoreFake CatalogSearchTrackings => trackingStore;

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => discoveryRepository;

    public MusicCatalogLookupHistoryRepositoryFake LookupHistoryRepository => lookupHistoryRepository;

    public DateTimeOffset Now { get; }

    public static MusicCatalogLookupAttemptedHandlerTestEnvironment Create() => new();

    public static MusicCatalogLookupAttemptedHandlerTestEnvironment WithAMusicBrainzResponse() => new();

    public static MusicCatalogLookupAttemptedHandlerTestEnvironment WithMultipleTrackingsForTheSameMusicCatalogId()
    {
        var env = WithAMusicBrainzResponse();
        env.CatalogSearchTrackings.Seed(new CatalogSearchTracking(
            MusicSearchCriteria.ByQuery("rare unknown song live", SearchTypesFilter.Tracks),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 1, 0, TimeSpan.Zero)));
        return env;
    }

    public static MusicCatalogLookupAttemptedHandlerTestEnvironment WithAPlaybackReferencesResponseAfterResolvedMetadata() => WithAMusicBrainzResponse();

    public static MusicCatalogLookupAttemptedHandlerTestEnvironment WithADuplicateMusicBrainzResponse() => WithAMusicBrainzResponse();

    public void ClearSearchTrackings() => trackingStore.Clear();

    public async Task SeedKnownTrackRequestAsync(string trackId = "track_1")
    {
        var knownItem = KnownCatalogId.ForTrack(TrackId.From(trackId));
        var loaded = await KnownItemDiscovery.LoadAsync(DiscoveryRepository, knownItem, CancellationToken.None);
        loaded.Aggregate.TrackRequested(
            TrackId.From(trackId),
            PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
            Now,
            CorrelationId.From("corr-track"));
        await loaded.Aggregate.SaveAsync(DiscoveryRepository, loaded.Stream, CancellationToken.None);
    }

    public async Task HandleMusicBrainzResponse()
    {
        var attempted = MusicCatalogLookupAttempted.Completed(MusicBrainzResponse());
        await HandleAttempted(attempted);
    }

    public async Task HandlePlaybackReferencesResponseAfterResolvedMetadata()
    {
        await HandleAttempted(MusicCatalogLookupAttempted.Completed(ResolvedMetadataResponse()));
        await HandleAttempted(MusicCatalogLookupAttempted.Completed(PlaybackReferencesResponse()));
    }

    public async Task HandleDuplicateMusicBrainzResponse()
    {
        var response = MusicBrainzResponse();
        await HandleAttempted(MusicCatalogLookupAttempted.Completed(response));
        await HandleAttempted(MusicCatalogLookupAttempted.Completed(response));
    }

    public async Task Handle(MusicCatalogMetadataFetched response)
    {
        await HandleAttempted(MusicCatalogLookupAttempted.Completed(response));
    }

    public async Task HandleAttempted(MusicCatalogLookupAttempted attempted)
    {
        await Handler.Handle(attempted, CancellationToken.None);

        var lookupId = MusicCatalogLookupId.From(attempted.MusicCatalogId);
        var events = LookupHistoryRepository.GetStoredEvents(lookupId)
            .Select((@event, index) => (index + 1, @event))
            .Where(item => item.Item1 > 0)
            .ToArray();

        var command = new MusicCatalogLookupHistoryChangedCommand(lookupId, events);
        await CatalogHandler.Handle(command, CancellationToken.None);
        await KnownTrackDiscoveryHandler.Handle(command, CancellationToken.None);
        await SearchDiscoveryHandler.Handle(command, CancellationToken.None);
    }

    public IReadOnlyList<IDomainEvent> StoredEvents(string criteria) =>
        criteria.StartsWith("artist:", StringComparison.Ordinal)
        || criteria.StartsWith("album:", StringComparison.Ordinal)
        || criteria.StartsWith("track:", StringComparison.Ordinal)
            ? discoveryRepository.GetStoredEvents(DiscoveryQueryKey.ToKnownCatalogItem(criteria))
            : discoveryRepository.GetStoredEvents(DiscoveryQueryKey.ToMusicSearchCriteria(criteria));

    public static MusicCatalogMetadataFetched MusicBrainzResponse() =>
        new(
            CommandId.For("ResolveMusicMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupSource.MusicBrainz,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"),
            [],
            [],
            new CatalogTrackHierarchy(ArtistId.From("artist_test_artist"), AlbumId.From("album_rare_album")),
            CorrelationId.From("corr-1"));

    private static MusicCatalogMetadataFetched ResolvedMetadataResponse() =>
        new(
            CommandId.For("ResolveMusicMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupSource.MusicBrainz,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            new SongMetadata("Resolved Song", "Resolved Artist", "isrc-1", "mbid-1", 123000, "Resolved Album", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"),
            [],
            [],
            new CatalogTrackHierarchy(ArtistId.From("artist_test_artist"), AlbumId.From("album_rare_album")),
            CorrelationId.From("corr-1"));

    public static MusicCatalogMetadataFetched PlaybackReferencesResponse() =>
        new(
            CommandId.For("LookupStreamingLocations:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupSource.Odesli,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
            null,
            [new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/track/1"), "apple-1")],
            [],
            new CatalogTrackHierarchy(ArtistId.From("artist_test_artist"), AlbumId.From("album_rare_album")),
            CorrelationId.From("corr-2"));

    private void SeedDiscovery(MusicSearchCriteria searchCriteria)
    {
        discoveryRepository.Seed(
            searchCriteria,
            new DiscoveryRequested(
                searchCriteria,
                null,
                1,
                10,
                Now,
                CorrelationId.From("corr-1")),
            new DiscoveryPlanned(
                searchCriteria,
                LookupPriorityBand.High,
                true,
                30,
                Now.AddSeconds(30),
                "Planner queued lookup",
                Now));
    }
}
