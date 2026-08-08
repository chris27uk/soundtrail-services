using System.Net;

namespace Soundtrail.Services.Tests.Integration.GetTrack.Api;

public sealed class TrackRouteExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_Ok_Is_Returned()
    {
        var trackId = TestTrackIds.Value("track-501");
        using var environment = GetTrackRouteTestEnvironment.ForExistingTrack(trackId);

        var response = await environment.Client.GetAsync($"/catalog/tracks/{trackId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
