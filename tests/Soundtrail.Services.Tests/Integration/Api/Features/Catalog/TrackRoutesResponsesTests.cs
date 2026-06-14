using FluentAssertions;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;
using System.Net;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Catalog;

public sealed class TrackRoutesResponsesTests
{
    [Fact]
    public async Task Given_A_Known_Track_When_Getting_The_Track_Then_Track_Metadata_Is_Returned()
    {
        await using var factory = new CatalogRoutesApiFactory();
        factory.CatalogReadPort.Track = ApiKnownTracks.MrBrightsideTrackDetails();

        var response = await factory.CreateClient().GetFromJsonAsync<TrackResponse>("/artists/artist_the_killers/albums/album_hot_fuss/tracks/track_mr_brightside");

        response!.Title.Should().Be("Mr. Brightside");
        response.AlbumName.Should().Be("Hot Fuss");
    }

    [Fact]
    public async Task Given_A_Track_That_Belongs_To_A_Different_Album_When_Getting_The_Track_Then_NotFound_Is_Returned()
    {
        await using var factory = new CatalogRoutesApiFactory();
        factory.CatalogReadPort.Track = ApiKnownTracks.MrBrightsideTrackDetails();

        var response = await factory.CreateClient().GetAsync("/artists/artist_the_killers/albums/album_wrong/tracks/track_mr_brightside");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed class TrackResponse
    {
        public string Title { get; set; } = string.Empty;
        public string AlbumName { get; set; } = string.Empty;
    }
}
