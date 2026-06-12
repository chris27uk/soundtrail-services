using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class EnrichmentResponseListenerTestEnvironment
{
    private EnrichmentResponseListenerTestEnvironment()
    {
        StreamStore = new MusicTrackStreamStoreFake();
        ProjectionStore = new MusicTrackProjectionStoreFake();
        SnapshotStore = new ProviderSnapshotStoreFake();
        Listener = new EnrichmentResponseListener(
            new ApplyEnrichmentResponseHandler(
                StreamStore,
                ProjectionStore,
                SnapshotStore));
    }

    public EnrichmentResponseListener Listener { get; }

    public MusicTrackStreamStoreFake StreamStore { get; }

    public MusicTrackProjectionStoreFake ProjectionStore { get; }

    public ProviderSnapshotStoreFake SnapshotStore { get; }

    public static EnrichmentResponseListenerTestEnvironment WithAMusicBrainzResponseDto() => new();

    public static EnrichmentResponseListenerTestEnvironment WithAPlaybackReferencesResponseAfterCanonicalMetadata() => new();

    public static EnrichmentResponseListenerTestEnvironment WithADuplicateMusicBrainzResponseDto() => new();

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
