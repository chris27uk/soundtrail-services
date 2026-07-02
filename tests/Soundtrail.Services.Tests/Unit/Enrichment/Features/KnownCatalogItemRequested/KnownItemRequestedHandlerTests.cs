using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using KnownTrackRequestedEvent = Soundtrail.Domain.Discovery.Events.KnownTrackRequested;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.KnownCatalogItemRequested;

public sealed class KnownItemRequestedHandlerTests
{
    [Fact]
    public async Task Given_A_Known_Artist_Request_When_Handled_Then_An_Artist_Catalog_Lookup_Requested_Event_Is_Stored()
    {
        var env = KnownItemRequestedHandlerTestEnvironment.Create();
        var request = env.ArtistRequest("artist_1");

        await env.ArtistHandler.Handle(request, CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForArtist(request.ArtistId))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ArtistCatalogLookupRequested>();

        env.CommandBus.SentCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Known_Album_Request_When_Handled_Then_An_Album_Catalog_Lookup_Requested_Event_Is_Stored()
    {
        var env = KnownItemRequestedHandlerTestEnvironment.Create();
        var request = env.AlbumRequest("artist_1", "album_1");

        await env.AlbumHandler.Handle(request, CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForAlbum(request.ArtistId, request.AlbumId))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<AlbumCatalogLookupRequested>();

        env.CommandBus.SentCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Known_Track_Request_When_Handled_Then_Known_Track_Requested_Is_Stored()
    {
        var env = KnownItemRequestedHandlerTestEnvironment.Create();
        var request = env.TrackRequest("track_1");

        await env.TrackHandler.Handle(request, CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForTrack(request.TrackId))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<KnownTrackRequestedEvent>();
    }

    [Fact]
    public async Task Given_An_Album_Catalog_Lookup_Requested_Event_When_Handled_Then_A_Lookup_Album_Metadata_Command_Is_Sent()
    {
        var env = KnownItemRequestedHandlerTestEnvironment.Create();
        var command = env.AlbumLookupRequestedCommand("artist_1", "album_1");

        await env.AlbumLookupDispatchHandler.Handle(command, CancellationToken.None);

        env.CommandBus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<LookupAlbumMetadataCommand>()
            .Which.AlbumId.Should().Be(AlbumId.From("album_1"));
    }

    [Fact]
    public async Task Given_An_Artist_Catalog_Lookup_Requested_Event_When_Handled_Then_A_Lookup_Artist_Metadata_Command_Is_Sent()
    {
        var env = KnownItemRequestedHandlerTestEnvironment.Create();
        var command = env.ArtistLookupRequestedCommand("artist_1");

        await env.ArtistLookupDispatchHandler.Handle(command, CancellationToken.None);

        env.CommandBus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<LookupArtistMetadataCommand>()
            .Which.ArtistId.Should().Be(ArtistId.From("artist_1"));
    }
}
