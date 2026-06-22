using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class ApplyEnrichmentResponseHandlerTestEnvironment
{
    private ApplyEnrichmentResponseHandlerTestEnvironment()
    {
        StreamStore = new MusicTrackStreamStoreFake();
        CatalogSearchTrackings = new CatalogSearchTrackingStoreFake();
        DiscoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        Handler = new ApplyEnrichmentResponseHandler(
            StreamStore,
            CatalogSearchTrackings,
            DiscoveryRepository);
    }

    public ApplyEnrichmentResponseHandler Handler { get; }

    public MusicTrackStreamStoreFake StreamStore { get; }

    public CatalogSearchTrackingStoreFake CatalogSearchTrackings { get; }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository { get; }

    public static ApplyEnrichmentResponseHandlerTestEnvironment WithAMusicBrainzResponse()
    {
        var env = new ApplyEnrichmentResponseHandlerTestEnvironment();
        env.CatalogSearchTrackings.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song"),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero)));
        return env;
    }

    public static ApplyEnrichmentResponseHandlerTestEnvironment WithMultipleTrackingsForTheSameMusicCatalogId()
    {
        var env = WithAMusicBrainzResponse();
        env.CatalogSearchTrackings.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song live"),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 1, 0, TimeSpan.Zero)));
        return env;
    }

    public static ApplyEnrichmentResponseHandlerTestEnvironment WithAPlaybackReferencesResponseAfterCanonicalMetadata() => WithAMusicBrainzResponse();

    public static ApplyEnrichmentResponseHandlerTestEnvironment WithADuplicateMusicBrainzResponse() => WithAMusicBrainzResponse();

    public Task HandleMusicBrainzResponse() => Handler.Handle(MusicBrainzResponse(), CancellationToken.None);

    public async Task HandlePlaybackReferencesResponseAfterCanonicalMetadata()
    {
        await Handler.Handle(CanonicalResponse(), CancellationToken.None);
        await Handler.Handle(PlaybackReferencesResponse(), CancellationToken.None);
    }

    public async Task HandleDuplicateMusicBrainzResponse()
    {
        var response = MusicBrainzResponse();
        await Handler.Handle(response, CancellationToken.None);
        await Handler.Handle(response, CancellationToken.None);
    }

    public Task Handle(EnrichmentResponse response) => Handler.Handle(response, CancellationToken.None);

    public static EnrichmentResponse MusicBrainzResponse() =>
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

    private static EnrichmentResponse CanonicalResponse() =>
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

    public static EnrichmentResponse PlaybackReferencesResponse() =>
        new(
            CommandId.For("ResolvePlaybackReferences:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.Odesli,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
            null,
            [new ExternalReference(ProviderName.AppleMusic, new Uri("https://music.apple.com/track/1"), "apple-1")],
            [],
            new CatalogTrackHierarchy(ArtistId.From("artist_test_artist"), AlbumId.From("album_rare_album")),
            CorrelationId.From("corr-2"));
}
