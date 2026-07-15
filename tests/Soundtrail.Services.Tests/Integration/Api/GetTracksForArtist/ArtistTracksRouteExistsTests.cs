using System.Net;

namespace Soundtrail.Services.Tests.Integration.Api.GetTracksForArtist;

public sealed class ArtistTracksRouteExistsTests
{
    [Fact]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_Ok_Is_Returned()
    {
        using var environment = GetTracksForArtistRouteTestEnvironment.ForExistingArtistTracks();

        var response = await environment.Client.GetAsync("/artists/artist-2501/tracks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
