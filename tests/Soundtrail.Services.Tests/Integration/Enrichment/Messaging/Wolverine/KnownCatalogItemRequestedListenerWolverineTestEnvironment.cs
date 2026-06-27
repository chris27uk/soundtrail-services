using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Ports;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

internal sealed class KnownCatalogItemRequestedListenerWolverineTestEnvironment
{
    private static readonly DateTimeOffset DefaultOccurredAt = new(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);
    private readonly LoadKnownCatalogTrackPortFake loadKnownCatalogTrackPort;
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;

    private KnownCatalogItemRequestedListenerWolverineTestEnvironment()
    {
        loadKnownCatalogTrackPort = new LoadKnownCatalogTrackPortFake();
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();

        Listener = new KnownCatalogItemRequestedListener(
            new KnownCatalogItemRequestedHandler(
                discoveryRepository,
                loadKnownCatalogTrackPort));
    }

    public KnownCatalogItemRequestedListener Listener { get; }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => discoveryRepository;

    public static KnownCatalogItemRequestedListenerWolverineTestEnvironment Create() => new();

    public void SeedTrack(
        string trackId,
        string[]? availableProviders = null,
        string? isrc = "isrc-1",
        string? title = "Song A",
        string? artist = "Artist A",
        string? album = "Album A")
    {
        loadKnownCatalogTrackPort.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From(trackId),
            title,
            artist,
            album,
            isrc,
            "mbid-1",
            123000,
            IsPlayable: false,
            availableProviders?.Select(ProviderName.From).ToArray() ?? [],
            ArtistId.From("artist_1"),
            AlbumId.From("album_1")));
    }

    public Task HandleArtistRequest() =>
        Listener.Handle(
            new KnownCatalogItemRequestedDto(
                ArtistId: "artist_1",
                AlbumId: null,
                TrackId: null,
                Playback: "spotify,appleMusic,youtubeMusic",
                TrustLevel: 0,
                RiskScore: 0,
                OccurredAt: DefaultOccurredAt,
                CorrelationId: "corr-artist"),
            null!);

    public Task HandleAlbumRequest() =>
        Listener.Handle(
            new KnownCatalogItemRequestedDto(
                ArtistId: null,
                AlbumId: "album_1",
                TrackId: null,
                Playback: "spotify,appleMusic,youtubeMusic",
                TrustLevel: 0,
                RiskScore: 0,
                OccurredAt: DefaultOccurredAt,
                CorrelationId: "corr-album"),
            null!);

    public Task HandleTrackRequest(string trackId) =>
        Listener.Handle(
            new KnownCatalogItemRequestedDto(
                ArtistId: null,
                AlbumId: null,
                TrackId: trackId,
                Playback: "spotify,appleMusic,youtubeMusic",
                TrustLevel: 0,
                RiskScore: 0,
                OccurredAt: DefaultOccurredAt,
                CorrelationId: "corr-track"),
            null!);

    private sealed class LoadKnownCatalogTrackPortFake : ILoadKnownCatalogTrackPort
    {
        private readonly Dictionary<string, LocalMusicTrackSearchResult> byTrackId = [];

        public void Seed(LocalMusicTrackSearchResult track) => byTrackId[track.MusicCatalogId.Value] = track;

        public Task<LocalMusicTrackSearchResult?> LoadAsync(TrackId trackId, CancellationToken cancellationToken) =>
            Task.FromResult(byTrackId.TryGetValue(trackId.Value, out var track) ? track : null);
    }
}
