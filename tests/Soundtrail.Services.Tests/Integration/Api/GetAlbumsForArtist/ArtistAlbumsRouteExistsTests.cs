using System.Net;

namespace Soundtrail.Services.Tests.Integration.Api.GetAlbumsForArtist;

public sealed class ArtistAlbumsRouteExistsTests
{
    [Fact]
    public async Task Given_Existing_Artist_Albums_When_Requesting_The_Artist_Albums_Then_Ok_Is_Returned()
    {
        using var environment = GetAlbumsForArtistRouteTestEnvironment.ForExistingArtistAlbums();

        var response = await environment.Client.GetAsync("/artists/artist-1901/albums");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
