using System.Net;

namespace Soundtrail.Services.Tests.Integration.GetTracksForArtist.Api;

public sealed class ArtistTracksRouteExistsTests
{
    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_Ok_Is_Returned()
    {
        var artistId = "artist-2501";
        using var environment = GetTracksForArtistRouteTestEnvironment.ForExistingArtistTracks(artistId);

        var response = await environment.Client.GetAsync($"/catalog/artists/{artistId}/tracks", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
