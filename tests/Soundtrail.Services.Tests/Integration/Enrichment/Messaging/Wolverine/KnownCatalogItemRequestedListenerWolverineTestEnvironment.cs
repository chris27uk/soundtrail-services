using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.Adapters;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

internal sealed class KnownCatalogItemRequestedListenerWolverineTestEnvironment
{
    private static readonly DateTimeOffset DefaultOccurredAt = new(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;

    private KnownCatalogItemRequestedListenerWolverineTestEnvironment()
    {
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();

        ArtistListener = new KnownArtistRequestedListener(
            new KnownArtistRequestedHandler(discoveryRepository));
        AlbumListener = new KnownAlbumRequestedListener(
            new KnownAlbumRequestedHandler(discoveryRepository));
        TrackListener = new KnownTrackRequestedListener(
            new KnownTrackRequestedHandler(discoveryRepository));
    }

    public KnownArtistRequestedListener ArtistListener { get; }

    public KnownAlbumRequestedListener AlbumListener { get; }

    public KnownTrackRequestedListener TrackListener { get; }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => discoveryRepository;

    public static KnownCatalogItemRequestedListenerWolverineTestEnvironment Create() => new();

    public Task HandleArtistRequest() =>
        ArtistListener.Handle(
            new KnownArtistRequestedDto(
                ArtistId: "artist_1",
                OccurredAt: DefaultOccurredAt,
                CorrelationId: "corr-artist"),
            null!);

    public Task HandleAlbumRequest() =>
        AlbumListener.Handle(
            new KnownAlbumRequestedDto(
                AlbumId: "album_1",
                OccurredAt: DefaultOccurredAt,
                CorrelationId: "corr-album"),
            null!);

    public Task HandleTrackRequest(string trackId) =>
        TrackListener.Handle(
            new KnownTrackRequestedDto(
                TrackId: trackId,
                Playback: "spotify,appleMusic,youtubeMusic",
                OccurredAt: DefaultOccurredAt,
                CorrelationId: "corr-track"),
            null!);
}
