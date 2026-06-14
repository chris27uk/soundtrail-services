using FluentAssertions;
using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Services.Api.Features.Albums;
using Soundtrail.Services.Api.Features.Albums.GetAlbum;
using Soundtrail.Services.Api.Features.Albums.ListTracksByAlbum;
using Soundtrail.Services.Api.Features.Tracks;
using Soundtrail.Services.Api.Features.Tracks.GetTrack;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.CatalogBrowsing;

public sealed class HierarchyValidationTests
{
    [Fact]
    public async Task Given_An_Album_That_Belongs_To_A_Different_Artist_When_Getting_The_Album_Then_The_Handler_Delegates_To_The_Port_Without_Post_Read_Hierarchy_Logic()
    {
        var port = new FakeCatalogReadPort
        {
            Album = ApiKnownTracks.HotFussAlbum()
        };
        var handler = new GetAlbumHandler(port);

        var response = await handler.Handle(new GetAlbumCommand(ArtistId.From("artist_the_killers"), AlbumId.From("album_hot_fuss")));

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_A_Known_Album_When_Listing_Tracks_By_Album_Then_The_Handler_Returns_The_Port_Response()
    {
        var port = new FakeCatalogReadPort
        {
            Album = ApiKnownTracks.HotFussAlbum(),
            AlbumTracks = [ApiKnownTracks.MrBrightsideTrackSummary()]
        };
        var handler = new ListTracksByAlbumHandler(port);

        var response = await handler.Handle(new ListTracksByAlbumCommand(ArtistId.From("artist_the_killers"), AlbumId.From("album_hot_fuss")));

        response.Should().NotBeNull();
        response!.Tracks.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Known_Track_When_Getting_The_Track_Then_The_Handler_Returns_The_Port_Response()
    {
        var port = new FakeCatalogReadPort
        {
            Track = ApiKnownTracks.MrBrightsideTrackDetails()
        };
        var handler = new GetTrackHandler(port);

        var response = await handler.Handle(new GetTrackCommand(
            ArtistId.From("artist_the_killers"),
            AlbumId.From("album_hot_fuss"),
            TrackId.From("track_mr_brightside")));

        response.Should().NotBeNull();
    }
}
