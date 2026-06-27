using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class KnownItemRequestedHandlerTestEnvironment
{
    private KnownItemRequestedHandlerTestEnvironment()
    {
        DiscoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        ArtistHandler = new KnownArtistRequestedHandler(DiscoveryRepository);
        AlbumHandler = new KnownAlbumRequestedHandler(DiscoveryRepository);
        TrackHandler = new KnownTrackRequestedHandler(DiscoveryRepository);
    }

    public KnownArtistRequestedHandler ArtistHandler { get; }

    public KnownAlbumRequestedHandler AlbumHandler { get; }

    public KnownTrackRequestedHandler TrackHandler { get; }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository { get; }

    public static KnownItemRequestedHandlerTestEnvironment Create() => new();

    public KnownArtistRequested ArtistRequest(string artistId) =>
        new(
            ArtistId.From(artistId),
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-artist"));

    public KnownAlbumRequested AlbumRequest(string albumId) =>
        new(
            AlbumId.From(albumId),
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-album"));

    public KnownTrackRequested TrackRequest(string trackId) =>
        new(
            TrackId.From(trackId),
            PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
            new DateTimeOffset(2026, 6, 27, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-track"));
}
