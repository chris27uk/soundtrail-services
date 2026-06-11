using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Events;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class MusicTrackEventListenerTestEnvironment
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);

    private MusicTrackEventListenerTestEnvironment()
    {
        Listener = new MusicTrackEventListener();
    }

    public MusicTrackEventListener Listener { get; }

    public static MusicTrackEventListenerTestEnvironment WithPlaybackReferencesResolutionRequiredMessage() => new();

    public object HandlePlaybackReferencesResolutionRequired() =>
        Listener.Handle(
            new PlaybackReferencesResolutionRequiredMessageDto(
                "mc_track_1",
                LookupPriorityBand.High,
                "corr-1",
                ProviderName.MusicBrainz.Value,
                ObservedAt,
                new PlaybackReferenceSearchTermDto("isrc-1", null, null, null)),
            null!);
}
