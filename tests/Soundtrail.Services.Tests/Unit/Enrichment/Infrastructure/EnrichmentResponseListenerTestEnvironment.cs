using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class EnrichmentResponseListenerTestEnvironment
{
    private EnrichmentResponseListenerTestEnvironment()
    {
        StreamStore = new MusicTrackStreamStoreFake();
        SnapshotStore = new ProviderSnapshotStoreFake();
        CatalogSearchTrackings = new CatalogSearchTrackingStoreFake();
        DiscoveryStatus = new InMemoryUpsertCatalogSearchStatus();
        Listener = new EnrichmentResponseListener(
            new ApplyEnrichmentResponseHandler(
                StreamStore,
                SnapshotStore,
                CatalogSearchTrackings,
                DiscoveryStatus));
    }

    public EnrichmentResponseListener Listener { get; }

    public MusicTrackStreamStoreFake StreamStore { get; }

    public ProviderSnapshotStoreFake SnapshotStore { get; }

    public CatalogSearchTrackingStoreFake CatalogSearchTrackings { get; }

    public InMemoryUpsertCatalogSearchStatus DiscoveryStatus { get; }

    public static EnrichmentResponseListenerTestEnvironment WithAMusicBrainzResponseDto()
    {
        var env = new EnrichmentResponseListenerTestEnvironment();
        env.CatalogSearchTrackings.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song"),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero)));
        return env;
    }

    public static EnrichmentResponseListenerTestEnvironment WithMultipleTrackingsForTheSameMusicCatalogId()
    {
        var env = WithAMusicBrainzResponseDto();
        env.CatalogSearchTrackings.Seed(new CatalogSearchTracking(
            CatalogSearchCriteria.Search("track", "rare unknown song live"),
            MusicCatalogId.From("mc_track_1"),
            new DateTimeOffset(2026, 6, 8, 12, 1, 0, TimeSpan.Zero)));
        return env;
    }

    public static EnrichmentResponseListenerTestEnvironment WithAPlaybackReferencesResponseAfterCanonicalMetadata() => WithAMusicBrainzResponseDto();

    public static EnrichmentResponseListenerTestEnvironment WithADuplicateMusicBrainzResponseDto() => WithAMusicBrainzResponseDto();

    public async Task HandleMusicBrainzResponse()
    {
        await Listener.Handle(MusicBrainzResponseDto(), null!);
    }

    public async Task HandlePlaybackReferencesResponseAfterCanonicalMetadata()
    {
        await Listener.Handle(CanonicalResponseDto(), null!);
        await Listener.Handle(PlaybackReferencesResponseDto(), null!);
    }

    public async Task HandleDuplicateMusicBrainzResponse()
    {
        var dto = MusicBrainzResponseDto();
        await Listener.Handle(dto, null!);
        await Listener.Handle(dto, null!);
    }

    private static EnrichmentResponseDto MusicBrainzResponseDto() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1").Value,
            "mc_track_1",
            ProviderName.MusicBrainz.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            new SongMetadataDto("Song A", "Artist A", "isrc-1", "mbid-1", 123000),
            [],
            "corr-1");

    private static EnrichmentResponseDto CanonicalResponseDto() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1").Value,
            "mc_track_1",
            ProviderName.MusicBrainz.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 0, 0, TimeSpan.Zero),
            new SongMetadataDto("Canonical Song", "Canonical Artist", "isrc-1", "mbid-1", 123000),
            [],
            "corr-1");

    private static EnrichmentResponseDto PlaybackReferencesResponseDto() =>
        new(
            CommandId.For("ResolvePlaybackReferences:mc_track_1").Value,
            "mc_track_1",
            ProviderName.Odesli.Value,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 8, 12, 2, 0, TimeSpan.Zero),
            null,
            [new ExternalReferenceDto(ProviderName.AppleMusic.Value, new Uri("https://music.apple.com/track/1"), "apple-1")],
            "corr-2");
}
