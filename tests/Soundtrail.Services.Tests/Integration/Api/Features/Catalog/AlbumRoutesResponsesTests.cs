using FluentAssertions;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;
using System.Net;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Catalog;

public sealed class AlbumRoutesResponsesTests
{
    [Fact]
    public async Task Given_A_Known_Album_When_Getting_The_Album_Then_Album_Metadata_And_Tracks_Are_Returned()
    {
        await using var factory = new CatalogRoutesApiFactory();
        factory.CatalogReadPort.Album = ApiKnownTracks.HotFussAlbum();

        var response = await factory.CreateClient().GetFromJsonAsync<AlbumResponse>("/artists/artist_the_killers/albums/album_hot_fuss");

        response!.Name.Should().Be("Hot Fuss");
        response.Tracks.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_An_Album_That_Belongs_To_A_Different_Artist_When_Getting_The_Album_Then_NotFound_Is_Returned()
    {
        await using var factory = new CatalogRoutesApiFactory();
        factory.CatalogReadPort.Album = ApiKnownTracks.HotFussAlbum();

        var response = await factory.CreateClient().GetAsync("/artists/artist_wrong/albums/album_hot_fuss");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Given_A_Known_Album_When_Getting_Album_Tracks_Then_Tracks_Are_Returned()
    {
        await using var factory = new CatalogRoutesApiFactory();
        factory.CatalogReadPort.Album = ApiKnownTracks.HotFussAlbum();
        factory.CatalogReadPort.AlbumTracks = [ApiKnownTracks.MrBrightsideTrackSummary()];

        var response = await factory.CreateClient().GetFromJsonAsync<AlbumTracksResponse>("/artists/artist_the_killers/albums/album_hot_fuss/tracks");

        response!.Tracks.Should().ContainSingle();
        response.Tracks[0].Title.Should().Be("Mr. Brightside");
    }

    private sealed class AlbumResponse
    {
        public string Name { get; set; } = string.Empty;
        public List<TrackResponse> Tracks { get; set; } = [];
    }

    private sealed class AlbumTracksResponse
    {
        public List<TrackResponse> Tracks { get; set; } = [];
    }

    private sealed class TrackResponse
    {
        public string Title { get; set; } = string.Empty;
    }
}
