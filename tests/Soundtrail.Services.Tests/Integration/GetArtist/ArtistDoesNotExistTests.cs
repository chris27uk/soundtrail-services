using System.Net;

namespace Soundtrail.Services.Tests.Integration.GetArtist;

public sealed class ArtistDoesNotExistTests
{
    [Fact]
    public async Task Given_A_Missing_Artist_When_Requesting_The_Artist_Then_Not_Found_Is_Returned()
    {
        using var environment = MissingArtistIntegrationTestEnvironment.ForMissingArtist();

        var response = await environment.GetAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
