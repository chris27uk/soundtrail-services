using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

internal sealed class KnownCatalogItemRequestedListenerWolverineTestEnvironment
{
    private static readonly DateTimeOffset DefaultOccurredAt = new(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogSearchDiscoveryRepositoryFake discoveryRepository;

    private KnownCatalogItemRequestedListenerWolverineTestEnvironment()
    {
        discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();

        Listener = new KnownCatalogItemRequestedListener(
            new KnownArtistRequestedHandler(discoveryRepository),
            new KnownAlbumRequestedHandler(discoveryRepository),
            new KnownTrackRequestedHandler(discoveryRepository));
    }

    public KnownCatalogItemRequestedListener Listener { get; }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository => discoveryRepository;

    public static KnownCatalogItemRequestedListenerWolverineTestEnvironment Create() => new();

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
}
