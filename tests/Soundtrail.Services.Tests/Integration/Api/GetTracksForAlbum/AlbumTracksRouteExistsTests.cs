using System.Net;

namespace Soundtrail.Services.Tests.Integration.Api.GetTracksForAlbum;

public sealed class AlbumTracksRouteExistsTests
{
    [Fact]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_Ok_Is_Returned()
    {
        using var environment = GetTracksForAlbumRouteTestEnvironment.ForExistingAlbumTracks();

        var response = await environment.Client.GetAsync("/catalog/artists/artist-801/albums/album-901/tracks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
