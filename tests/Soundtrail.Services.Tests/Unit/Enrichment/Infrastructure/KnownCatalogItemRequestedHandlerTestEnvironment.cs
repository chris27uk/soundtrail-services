using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Ports;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class KnownCatalogItemRequestedHandlerTestEnvironment
{
    private readonly LoadKnownCatalogTrackPortFake loadTrackPort;

    private KnownCatalogItemRequestedHandlerTestEnvironment()
    {
        DiscoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        loadTrackPort = new LoadKnownCatalogTrackPortFake();
        Handler = new KnownCatalogItemRequestedHandler(DiscoveryRepository, loadTrackPort);
    }

    public KnownCatalogItemRequestedHandler Handler { get; }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository { get; }

    public static KnownCatalogItemRequestedHandlerTestEnvironment Create() => new();

    public void SeedTrack(LocalMusicTrackSearchResult track) => loadTrackPort.Seed(track);

    public KnownCatalogItemRequested ArtistRequest(string artistId) =>
        new(
            KnownCatalogItem.ForArtist(ArtistId.From(artistId)),
            PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
            0,
            0,
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-artist"));

    public KnownCatalogItemRequested AlbumRequest(string albumId) =>
        new(
            KnownCatalogItem.ForAlbum(AlbumId.From(albumId)),
            PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
            0,
            0,
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-album"));

    public KnownCatalogItemRequested TrackRequest(string trackId) =>
        new(
            KnownCatalogItem.ForTrack(TrackId.From(trackId)),
            PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
            0,
            0,
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-track"));

    private sealed class LoadKnownCatalogTrackPortFake : ILoadKnownCatalogTrackPort
    {
        private readonly Dictionary<string, LocalMusicTrackSearchResult> byTrackId = [];

        public void Seed(LocalMusicTrackSearchResult track) => byTrackId[track.MusicCatalogId.Value] = track;

        public Task<LocalMusicTrackSearchResult?> LoadAsync(TrackId trackId, CancellationToken cancellationToken) =>
            Task.FromResult(byTrackId.TryGetValue(trackId.Value, out var track) ? track : null);
    }
}
