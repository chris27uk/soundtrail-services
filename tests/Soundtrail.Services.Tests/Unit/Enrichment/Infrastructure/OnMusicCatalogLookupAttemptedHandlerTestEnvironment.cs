using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class MusicCatalogLookupAttemptedHandlerTestEnvironment
{
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;
    private readonly CatalogSearchTrackingStoreFake trackingStore;

    private MusicCatalogLookupAttemptedHandlerTestEnvironment()
    {
        Now = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
        StreamStore = new MusicTrackStreamStoreFake();
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        trackingStore = new CatalogSearchTrackingStoreFake();
        trackingStore.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song"),
            MusicCatalogId.From("mc_track_1"),
            Now));
        trackingStore.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Artist(ArtistId.From("artist_1")),
            MusicCatalogId.From("mc_track_1"),
            Now));

        SeedDiscovery(CatalogSearchCriteria.Search("track", "rare unknown song"));
        SeedDiscovery(CatalogSearchCriteria.Artist(ArtistId.From("artist_1")));

        Handler = new MusicCatalogLookupAttemptedHandler(
            StreamStore,
            trackingStore,
            discoveryRepository);
    }

    public MusicCatalogLookupAttemptedHandler Handler { get; }

    public MusicTrackStreamStoreFake StreamStore { get; }

    public CatalogSearchTrackingStoreFake CatalogSearchTrackings => trackingStore;

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => discoveryRepository;

    public DateTimeOffset Now { get; }

    public static MusicCatalogLookupAttemptedHandlerTestEnvironment Create() => new();

    public static MusicCatalogLookupAttemptedHandlerTestEnvironment WithAMusicBrainzResponse() => new();

    public static MusicCatalogLookupAttemptedHandlerTestEnvironment WithMultipleTrackingsForTheSameMusicCatalogId()
    {
        var env = WithAMusicBrainzResponse();
        env.CatalogSearchTrackings.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song live"),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 1, 0, TimeSpan.Zero)));
        return env;
    }

    public static MusicCatalogLookupAttemptedHandlerTestEnvironment WithAPlaybackReferencesResponseAfterCanonicalMetadata() => WithAMusicBrainzResponse();

    public static MusicCatalogLookupAttemptedHandlerTestEnvironment WithADuplicateMusicBrainzResponse() => WithAMusicBrainzResponse();

    public Task HandleMusicBrainzResponse() => Handler.Handle(MusicCatalogLookupAttempted.Completed(MusicBrainzResponse()), CancellationToken.None);

    public async Task HandlePlaybackReferencesResponseAfterCanonicalMetadata()
    {
        await Handler.Handle(MusicCatalogLookupAttempted.Completed(CanonicalResponse()), CancellationToken.None);
        await Handler.Handle(MusicCatalogLookupAttempted.Completed(PlaybackReferencesResponse()), CancellationToken.None);
    }

    public async Task HandleDuplicateMusicBrainzResponse()
    {
        var response = MusicBrainzResponse();
        await Handler.Handle(MusicCatalogLookupAttempted.Completed(response), CancellationToken.None);
        await Handler.Handle(MusicCatalogLookupAttempted.Completed(response), CancellationToken.None);
    }

    public Task Handle(MusicCatalogMetadataFetched response) =>
        Handler.Handle(MusicCatalogLookupAttempted.Completed(response), CancellationToken.None);

    public IReadOnlyList<IDomainEvent> StoredEvents(string criteria) =>
        discoveryRepository.GetStoredEvents(CatalogSearchCriteria.From(criteria));

    public static MusicCatalogMetadataFetched MusicBrainzResponse() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"),
            [],
            [],
            new CatalogTrackHierarchy(ArtistId.From("artist_test_artist"), AlbumId.From("album_rare_album")),
            CorrelationId.From("corr-1"));

    private static MusicCatalogMetadataFetched CanonicalResponse() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            new SongMetadata("Canonical Song", "Canonical Artist", "isrc-1", "mbid-1", 123000, "Canonical Album", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"),
            [],
            [],
            new CatalogTrackHierarchy(ArtistId.From("artist_test_artist"), AlbumId.From("album_rare_album")),
            CorrelationId.From("corr-1"));

    public static MusicCatalogMetadataFetched PlaybackReferencesResponse() =>
        new(
            CommandId.For("LookupStreamingLocations:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.Odesli,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
            null,
            [new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/track/1"), "apple-1")],
            [],
            new CatalogTrackHierarchy(ArtistId.From("artist_test_artist"), AlbumId.From("album_rare_album")),
            CorrelationId.From("corr-2"));

    private void SeedDiscovery(CatalogSearchCriteria criteria)
    {
        discoveryRepository.Seed(
            criteria,
            new DiscoveryRequested(
                criteria,
                NormalizedSearchQuery.FromText("rare unknown song"),
                1,
                10,
                Now,
                CorrelationId.From("corr-1")),
            new DiscoveryPlanned(
                criteria,
                LookupPriorityBand.High,
                true,
                30,
                Now.AddSeconds(30),
                "Planner queued lookup",
                Now),
            new DiscoveryStarted(
                criteria,
                LookupPriorityBand.High,
                true,
                "Lookup started",
                Now));
    }
}
