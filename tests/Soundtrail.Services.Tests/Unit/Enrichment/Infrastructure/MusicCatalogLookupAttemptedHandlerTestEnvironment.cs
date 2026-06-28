using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class MusicCatalogLookupAttemptedHandlerTestEnvironment
{
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;
    private readonly CatalogSearchTrackingStoreFake trackingStore;
    private readonly CommandBusFake commandBus;

    private MusicCatalogLookupAttemptedHandlerTestEnvironment()
    {
        Now = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
        StreamStore = new MusicTrackStreamStoreFake();
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        trackingStore = new CatalogSearchTrackingStoreFake();
        commandBus = new CommandBusFake();
        trackingStore.Seed(new CatalogSearchTracking(
            MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks),
            MusicCatalogId.From("mc_track_1"),
            Now));

        SeedDiscovery(MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks));

        Handler = new MusicCatalogLookupAttemptedHandler(commandBus);
        CatalogHandler = new ApplyMusicCatalogLookupAttemptedToCatalogHandler(
            StreamStore);
        DiscoveryHandler = new ApplyMusicCatalogLookupAttemptedToDiscoveryHandler(
            trackingStore,
            discoveryRepository);
    }

    public MusicCatalogLookupAttemptedHandler Handler { get; }

    public ApplyMusicCatalogLookupAttemptedToCatalogHandler CatalogHandler { get; }

    public ApplyMusicCatalogLookupAttemptedToDiscoveryHandler DiscoveryHandler { get; }

    public MusicTrackStreamStoreFake StreamStore { get; }

    public CatalogSearchTrackingStoreFake CatalogSearchTrackings => trackingStore;

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => discoveryRepository;

    public CommandBusFake Bus => commandBus;

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

    public async Task HandleMusicBrainzResponse()
    {
        var attempted = MusicCatalogLookupAttempted.Completed(MusicBrainzResponse());
        await CatalogHandler.Handle(new ApplyMusicCatalogLookupAttemptedToCatalogCommand(attempted), CancellationToken.None);
        await DiscoveryHandler.Handle(new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(attempted), CancellationToken.None);
    }

    public async Task HandlePlaybackReferencesResponseAfterResolvedMetadata()
    {
        await CatalogHandler.Handle(new ApplyMusicCatalogLookupAttemptedToCatalogCommand(MusicCatalogLookupAttempted.Completed(ResolvedMetadataResponse())), CancellationToken.None);
        await DiscoveryHandler.Handle(new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(MusicCatalogLookupAttempted.Completed(ResolvedMetadataResponse())), CancellationToken.None);
        await CatalogHandler.Handle(new ApplyMusicCatalogLookupAttemptedToCatalogCommand(MusicCatalogLookupAttempted.Completed(PlaybackReferencesResponse())), CancellationToken.None);
        await DiscoveryHandler.Handle(new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(MusicCatalogLookupAttempted.Completed(PlaybackReferencesResponse())), CancellationToken.None);
    }

    public async Task HandleDuplicateMusicBrainzResponse()
    {
        var response = MusicBrainzResponse();
        await CatalogHandler.Handle(new ApplyMusicCatalogLookupAttemptedToCatalogCommand(MusicCatalogLookupAttempted.Completed(response)), CancellationToken.None);
        await DiscoveryHandler.Handle(new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(MusicCatalogLookupAttempted.Completed(response)), CancellationToken.None);
        await CatalogHandler.Handle(new ApplyMusicCatalogLookupAttemptedToCatalogCommand(MusicCatalogLookupAttempted.Completed(response)), CancellationToken.None);
        await DiscoveryHandler.Handle(new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(MusicCatalogLookupAttempted.Completed(response)), CancellationToken.None);
    }

    public async Task Handle(MusicCatalogMetadataFetched response)
    {
        var attempted = MusicCatalogLookupAttempted.Completed(response);
        await CatalogHandler.Handle(new ApplyMusicCatalogLookupAttemptedToCatalogCommand(attempted), CancellationToken.None);
        await DiscoveryHandler.Handle(new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(attempted), CancellationToken.None);
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
