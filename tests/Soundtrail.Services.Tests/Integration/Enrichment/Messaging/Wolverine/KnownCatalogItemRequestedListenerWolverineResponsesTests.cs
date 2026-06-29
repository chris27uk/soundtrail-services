using FluentAssertions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using KnownTrackRequestedEvent = Soundtrail.Domain.Discovery.Events.KnownTrackRequested;

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
            .GetStoredEvents(KnownCatalogItem.ForAlbum(ArtistId.From("artist_1"), AlbumId.From("album_1")))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<AlbumCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_A_Known_Track_Request_When_Handled_Then_Known_Track_Requested_Is_Stored()
    {
        var env = KnownCatalogItemRequestedListenerWolverineTestEnvironment.Create();

        await env.HandleTrackRequest("track_1");

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForTrack(TrackId.From("track_1")))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<KnownTrackRequestedEvent>();
    }
}
