using System.Net;

namespace Soundtrail.Services.Tests.Integration.Api.GetArtist;

public sealed class ArtistRouteExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_Ok_Is_Returned()
    {
        using var environment = GetArtistRouteTestEnvironment.ForExistingArtist();

        var response = await environment.Client.GetAsync("/catalog/artists/artist-701");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
