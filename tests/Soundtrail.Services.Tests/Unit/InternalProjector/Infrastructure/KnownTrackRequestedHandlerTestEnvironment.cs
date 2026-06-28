using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Support;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using KnownTrackRequestedEvent = Soundtrail.Domain.Discovery.Events.KnownTrackRequested;

namespace Soundtrail.Services.Tests.Unit.InternalProjector.Infrastructure;

internal sealed class KnownTrackRequestedHandlerTestEnvironment
{
    private readonly LoadKnownTrackRequestedMusicTrackPortFake loadTrackPort;

    private KnownTrackRequestedHandlerTestEnvironment()
    {
        loadTrackPort = new LoadKnownTrackRequestedMusicTrackPortFake();
        Bus = new CommandBusFake();
        Handler = new KnownTrackRequestedHandler(loadTrackPort, Bus);
    }

    public KnownTrackRequestedHandler Handler { get; }

    public CommandBusFake Bus { get; }

    public static KnownTrackRequestedHandlerTestEnvironment Create() => new();

    public void TrackIsMissing() => loadTrackPort.ReturnMissing();

    public void TrackRequiresStreamingLocations() =>
        loadTrackPort.ReturnAvailable(
            KnownTrackRequestedMusicTrack.Available(
                MusicCatalogId.From("track_1"),
                "Song A",
                "Artist A",
                "Album A",
                "isrc-1",
                [],
                ArtistId.From("artist_1"),
                AlbumId.From("album_1")));

    public KnownTrackRequestedCommand Command() =>
        new(
            KnownCatalogItem.ForTrack(TrackId.From("track_1")),
            [new VersionedCatalogSearchDiscoveryEvent(
                1,
                new KnownTrackRequestedEvent(
                    TrackId.From("track_1"),
                    PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
                    Clock,
                    CorrelationId.From("corr-track")))]);

    private sealed class LoadKnownTrackRequestedMusicTrackPortFake : ILoadKnownTrackRequestedMusicTrackPort
    {
        private KnownTrackRequestedMusicTrack current = KnownTrackRequestedMusicTrack.Missing;

        public void ReturnMissing() => current = KnownTrackRequestedMusicTrack.Missing;

        public void ReturnAvailable(KnownTrackRequestedMusicTrack track) => current = track;

        public Task<KnownTrackRequestedMusicTrack> LoadAsync(TrackId trackId, CancellationToken cancellationToken)
        {
            _ = trackId;
            _ = cancellationToken;
            return Task.FromResult(current);
        }
    }

    private static readonly DateTimeOffset Clock = new(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);
}
