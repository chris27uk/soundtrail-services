using System.Net;

namespace Soundtrail.Services.Tests.Integration.Api.GetAlbum;

public sealed class AlbumRouteExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Album_When_Requesting_The_Album_Then_Ok_Is_Returned()
    {
        using var environment = GetAlbumRouteTestEnvironment.ForExistingAlbum();

        var response = await environment.Client.GetAsync("/catalog/artists/artist-301/albums/album-501");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
