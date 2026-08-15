using System.Net;
using Soundtrail.Services.Tests.Integration.Features.GetArtist;

namespace Soundtrail.Services.Tests.Integration.Features.GetArtist.Scenarios.Api;

public sealed class ArtistRouteExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Artist_When_Requesting_The_Artist_Then_Ok_Is_Returned()
    {
        var artistId = "artist-701";
        using var environment = GetArtistRouteTestEnvironment.ForExistingArtist(artistId);

        var response = await environment.Client.GetAsync($"/catalog/artists/{artistId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
