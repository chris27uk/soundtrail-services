using FluentAssertions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Messaging.Wolverine;

public sealed class KnownCatalogItemRequestedListenerWolverineResponsesTests
{
    [Fact]
    public async Task Given_A_Known_Artist_Request_When_Handled_Then_An_Artist_Catalog_Lookup_Requested_Event_Is_Stored()
    {
        var env = KnownCatalogItemRequestedListenerWolverineTestEnvironment.Create();

        await env.HandleArtistRequest();

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForArtist(ArtistId.From("artist_1")))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ArtistCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_A_Known_Album_Request_When_Handled_Then_An_Album_Catalog_Lookup_Requested_Event_Is_Stored()
    {
        var env = KnownCatalogItemRequestedListenerWolverineTestEnvironment.Create();

        await env.HandleAlbumRequest();

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForAlbum(AlbumId.From("album_1")))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<AlbumCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_A_Known_Track_Request_With_Missing_Providers_When_Handled_Then_Streaming_Locations_Required_Is_Stored()
    {
        var env = KnownCatalogItemRequestedListenerWolverineTestEnvironment.Create();
        env.SeedTrack("track_1");

        await env.HandleTrackRequest("track_1");

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForTrack(TrackId.From("track_1")))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<StreamingLocationsRequired>();
    }
}
