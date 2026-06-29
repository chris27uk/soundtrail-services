using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.GetAlbum;
using Soundtrail.Services.Api.Features.GetArtist;
using Soundtrail.Services.Api.Features.GetTrack;
using Soundtrail.Services.Api.Features.ListTracksByAlbum;
using Soundtrail.Services.Api.Features.ListTracksByArtist;
using Soundtrail.Services.Api.Features.SearchCatalog;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Handlers;

public sealed class ApiHandlerDispatchTests
{
    [Fact]
    public async Task GetArtistHandler_Sends_KnownArtistRequested()
    {
        var catalogReadPort = new FakeCatalogReadPort
        {
            Artist = new ArtistDetailsResponse(
                ArtistId.From("artist_1"),
                "Artist 1",
                [])
        };
        var bus = new CommandBusFake();
        var handler = new GetArtistHandler(catalogReadPort, bus);

        await handler.Handle(new GetArtistCommand(ArtistId.From("artist_1")), CancellationToken.None);

        bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<KnownArtistRequested>()
            .Which.ArtistId.Should().Be(ArtistId.From("artist_1"));
    }

    [Fact]
    public async Task GetAlbumHandler_Sends_KnownAlbumRequested()
    {
        var catalogReadPort = new FakeCatalogReadPort
        {
            Album = new AlbumDetailsResponse(
                ArtistId.From("artist_1"),
                "Artist 1",
                AlbumId.From("album_1"),
                "Album 1",
                null,
                [])
        };
        var bus = new CommandBusFake();
        var handler = new GetAlbumHandler(catalogReadPort, bus);

        await handler.Handle(
            new GetAlbumCommand(ArtistId.From("artist_1"), AlbumId.From("album_1")),
            CancellationToken.None);

        bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<KnownAlbumRequested>()
            .Which.Should().BeEquivalentTo(new
            {
                ArtistId = ArtistId.From("artist_1"),
                AlbumId = AlbumId.From("album_1")
            });
    }

    [Fact]
    public async Task GetTrackHandler_Sends_KnownTrackRequested_With_Playback()
    {
        var catalogReadPort = new FakeCatalogReadPort
        {
            Track = new TrackDetailsResponse(
                ArtistId.From("artist_1"),
                "Artist 1",
                AlbumId.From("album_1"),
                "Album 1",
                TrackId.From("track_1"),
                "Track 1",
                null,
                null,
                PlayabilityStatus.Unknown,
                [],
                [],
                [])
        };
        var bus = new CommandBusFake();
        var handler = new GetTrackHandler(catalogReadPort, bus);
        var playback = PlaybackProviderFilter.Parse("spotify,appleMusic");

        await handler.Handle(
            new GetTrackCommand(
                ArtistId.From("artist_1"),
                AlbumId.From("album_1"),
                TrackId.From("track_1"),
                playback),
            CancellationToken.None);

        bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<KnownTrackRequested>()
            .Which.Should().BeEquivalentTo(new
            {
                TrackId = TrackId.From("track_1"),
                Playback = playback
            });
    }

    [Fact]
    public async Task ListTracksByArtistHandler_Sends_KnownArtistRequested()
    {
        var catalogReadPort = new FakeCatalogReadPort
        {
            Artist = new ArtistDetailsResponse(
                ArtistId.From("artist_1"),
                "Artist 1",
                [])
        };
        var bus = new CommandBusFake();
        var handler = new ListTracksByArtistHandler(catalogReadPort, bus);

        await handler.Handle(new ListTracksByArtistCommand(ArtistId.From("artist_1")), CancellationToken.None);

        bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<KnownArtistRequested>()
            .Which.ArtistId.Should().Be(ArtistId.From("artist_1"));
    }

    [Fact]
    public async Task ListTracksByAlbumHandler_Sends_KnownAlbumRequested()
    {
        var catalogReadPort = new FakeCatalogReadPort
        {
            Album = new AlbumDetailsResponse(
                ArtistId.From("artist_1"),
                "Artist 1",
                AlbumId.From("album_1"),
                "Album 1",
                null,
                [])
        };
        var bus = new CommandBusFake();
        var handler = new ListTracksByAlbumHandler(catalogReadPort, bus);

        await handler.Handle(
            new ListTracksByAlbumCommand(ArtistId.From("artist_1"), AlbumId.From("album_1")),
            CancellationToken.None);

        bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<KnownAlbumRequested>()
            .Which.Should().BeEquivalentTo(new
            {
                ArtistId = ArtistId.From("artist_1"),
                AlbumId = AlbumId.From("album_1")
            });
    }

    [Fact]
    public async Task SearchCatalogHandler_Appends_DiscoveryRequested_When_Local_Result_Is_Incomplete()
    {
        var catalogSearch = new FakeCatalogSearchPort
        {
            Response = new LocalCatalogSearchResponse([], null, false)
        };
        var discoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        var handler = new SearchCatalogHandler(catalogSearch, discoveryRepository);
        var command = new SearchCatalogCommand(
            "artist song",
            SearchTypesFilter.Tracks,
            PlaybackProviderFilter.Parse("spotify"),
            SearchLimit.From(10),
            SearchOffset.From(0));

        await handler.Handle(command, CancellationToken.None);

        discoveryRepository.GetStoredEvents(command.ToMusicSearchTerm())
            .Should().ContainSingle()
            .Which.Should().BeOfType<Soundtrail.Domain.Discovery.Events.DiscoveryRequested>()
            .Which.Should().BeEquivalentTo(new
            {
                SearchCriteria = command.ToMusicSearchTerm(),
                Playback = command.Playback,
                TrustLevel = 0,
                RiskScore = 0
            });
    }
}
