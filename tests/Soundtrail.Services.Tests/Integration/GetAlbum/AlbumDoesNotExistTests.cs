using System.Net;

namespace Soundtrail.Services.Tests.Integration.GetAlbum;

public sealed class AlbumDoesNotExistTests
{
    [Fact]
    public async Task Given_A_Missing_Album_When_Requesting_The_Album_Then_Not_Found_Is_Returned()
    {
        using var environment = MissingAlbumIntegrationTestEnvironment.ForMissingAlbum();

        var response = await environment.GetAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
