using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.KnownCatalogItemRequested;

public sealed class KnownCatalogItemRequestedHandlerTests
{
    [Fact]
    public async Task Given_A_Known_Artist_Request_When_Handled_Then_An_Artist_Catalog_Lookup_Requested_Event_Is_Stored()
    {
        var env = KnownCatalogItemRequestedHandlerTestEnvironment.Create();
        var request = env.ArtistRequest("artist_1");

        await env.Handler.Handle(request, CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(request.KnownItem)
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ArtistCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_A_Known_Album_Request_When_Handled_Then_An_Album_Catalog_Lookup_Requested_Event_Is_Stored()
    {
        var env = KnownCatalogItemRequestedHandlerTestEnvironment.Create();
        var request = env.AlbumRequest("album_1");

        await env.Handler.Handle(request, CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(request.KnownItem)
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<AlbumCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_A_Known_Track_Request_With_Missing_Providers_When_Handled_Then_Streaming_Locations_Required_Is_Stored()
    {
        var env = KnownCatalogItemRequestedHandlerTestEnvironment.Create();
        env.SeedTrack(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("track_1"),
            "Song A",
            "Artist A",
            "Album A",
            "isrc-1",
            "mbid-1",
            123000,
            IsPlayable: false,
            [],
            ArtistId.From("artist_1"),
            AlbumId.From("album_1")));
        var request = env.TrackRequest("track_1");

        await env.Handler.Handle(request, CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(request.KnownItem)
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<StreamingLocationsRequired>();
    }
}
