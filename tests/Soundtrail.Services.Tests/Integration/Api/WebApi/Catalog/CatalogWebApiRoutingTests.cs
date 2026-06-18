using FluentAssertions;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;
using System.Net;

namespace Soundtrail.Services.Tests.Integration.Api.WebApi.Catalog;

public sealed class CatalogWebApiRoutingTests
{
    [Fact]
    public async Task Given_The_Get_Artist_Route_When_Requesting_A_Known_Artist_Then_It_Returns_Ok()
    {
        await using var env = CatalogWebApiTestEnvironment.Create();
        env.CatalogReadPort.Artist = ApiKnownTracks.TheKillersArtistDetails();

        var response = await env.Client.GetAsync("/artists/artist_the_killers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_The_List_Artist_Tracks_Route_When_Requesting_A_Known_Artist_Then_It_Returns_Ok()
    {
        await using var env = CatalogWebApiTestEnvironment.Create();
        env.CatalogReadPort.Artist = ApiKnownTracks.TheKillersArtistDetails();
        env.CatalogReadPort.ArtistTracks = [ApiKnownTracks.MrBrightsideTrackSummary()];

        var response = await env.Client.GetAsync("/artists/artist_the_killers/tracks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_The_Get_Album_Route_When_Requesting_A_Known_Album_Then_It_Returns_Ok()
    {
        await using var env = CatalogWebApiTestEnvironment.Create();
        env.CatalogReadPort.Album = ApiKnownTracks.HotFussAlbum();

        var response = await env.Client.GetAsync("/artists/artist_the_killers/albums/album_hot_fuss");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_The_List_Album_Tracks_Route_When_Requesting_A_Known_Album_Then_It_Returns_Ok()
    {
        await using var env = CatalogWebApiTestEnvironment.Create();
        env.CatalogReadPort.Album = ApiKnownTracks.HotFussAlbum();
        env.CatalogReadPort.AlbumTracks = [ApiKnownTracks.MrBrightsideTrackSummary()];

        var response = await env.Client.GetAsync("/artists/artist_the_killers/albums/album_hot_fuss/tracks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_The_Get_Track_Route_When_Requesting_A_Known_Track_Then_It_Returns_Ok()
    {
        await using var env = CatalogWebApiTestEnvironment.Create();
        env.CatalogReadPort.Track = ApiKnownTracks.MrBrightsideTrackDetails();

        var response = await env.Client.GetAsync("/artists/artist_the_killers/albums/album_hot_fuss/tracks/track_mr_brightside");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
