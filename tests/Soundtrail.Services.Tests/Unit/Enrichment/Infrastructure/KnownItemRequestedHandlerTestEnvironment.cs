using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Ports;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Ports;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class KnownItemRequestedHandlerTestEnvironment
{
    private KnownItemRequestedHandlerTestEnvironment()
    {
        DiscoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        CommandBus = new CommandBusFake();
        LoadKnownCatalogArtistPort = new LoadKnownCatalogArtistPortFake();
        LoadKnownCatalogAlbumPort = new LoadKnownCatalogAlbumPortFake();
        ArtistHandler = new KnownArtistRequestedHandler(DiscoveryRepository, LoadKnownCatalogArtistPort, CommandBus);
        AlbumHandler = new KnownAlbumRequestedHandler(DiscoveryRepository, LoadKnownCatalogAlbumPort, CommandBus);
        TrackHandler = new KnownTrackRequestedHandler(DiscoveryRepository);
    }

    public KnownArtistRequestedHandler ArtistHandler { get; }

    public KnownAlbumRequestedHandler AlbumHandler { get; }

    public KnownTrackRequestedHandler TrackHandler { get; }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository { get; }

    public CommandBusFake CommandBus { get; }

    public LoadKnownCatalogArtistPortFake LoadKnownCatalogArtistPort { get; }

    public LoadKnownCatalogAlbumPortFake LoadKnownCatalogAlbumPort { get; }

    public static KnownItemRequestedHandlerTestEnvironment Create() => new();

    public KnownArtistRequested ArtistRequest(string artistId)
    {
        LoadKnownCatalogArtistPort.Seed(ArtistId.From(artistId), "Artist 1", "mb-artist-1");
        return new KnownArtistRequested(
            ArtistId.From(artistId),
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-artist"));
    }

    public KnownAlbumRequested AlbumRequest(string artistId, string albumId)
    {
        LoadKnownCatalogAlbumPort.Seed(ArtistId.From(artistId), AlbumId.From(albumId), "Artist 1", "Album 1", "mb-artist-1", "mb-release-1");
        return new KnownAlbumRequested(
            ArtistId.From(artistId),
            AlbumId.From(albumId),
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-album"));
    }

    public KnownTrackRequested TrackRequest(string trackId) =>
        new(
            TrackId.From(trackId),
            PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-track"));

    internal sealed class LoadKnownCatalogArtistPortFake : ILoadKnownCatalogArtistPort
    {
        private readonly Dictionary<string, KnownCatalogArtistLookupData> items = new(StringComparer.Ordinal);

        public Task<KnownCatalogArtistLookupData?> LoadAsync(ArtistId artistId, CancellationToken cancellationToken) =>
            Task.FromResult(items.TryGetValue(artistId.Value, out var value) ? value : null);

        public void Seed(ArtistId artistId, string artistName, string? musicBrainzArtistId) =>
            items[artistId.Value] = new KnownCatalogArtistLookupData(artistName, musicBrainzArtistId);
    }

    internal sealed class LoadKnownCatalogAlbumPortFake : ILoadKnownCatalogAlbumPort
    {
        private readonly Dictionary<string, KnownCatalogAlbumLookupData> items = new(StringComparer.Ordinal);

        public Task<KnownCatalogAlbumLookupData?> LoadAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken) =>
            Task.FromResult(items.TryGetValue(KeyFor(artistId, albumId), out var value) ? value : null);

        public void Seed(
            ArtistId artistId,
            AlbumId albumId,
            string artistName,
            string albumTitle,
            string? musicBrainzArtistId,
            string? musicBrainzReleaseId) =>
            items[KeyFor(artistId, albumId)] = new KnownCatalogAlbumLookupData(
                artistName,
                albumTitle,
                musicBrainzArtistId,
                musicBrainzReleaseId);

        private static string KeyFor(ArtistId artistId, AlbumId albumId) => $"{artistId.Value}:{albumId.Value}";
    }
}
