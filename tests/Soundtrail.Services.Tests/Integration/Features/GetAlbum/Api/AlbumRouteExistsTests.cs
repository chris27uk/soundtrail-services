using System.Net;

namespace Soundtrail.Services.Tests.Integration.GetAlbum.Api;

public sealed class AlbumRouteExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_Ok_Is_Returned()
    {
        var artistId = "artist-301";
        var albumId = "album-501";
        using var environment = GetAlbumRouteTestEnvironment.ForExistingAlbum(artistId, albumId);

        var response = await environment.Client.GetAsync($"/catalog/artists/{artistId}/albums/{albumId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
