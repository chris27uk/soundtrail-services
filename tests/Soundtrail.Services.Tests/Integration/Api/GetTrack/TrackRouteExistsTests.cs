using System.Net;

namespace Soundtrail.Services.Tests.Integration.Api.GetTrack;

public sealed class TrackRouteExistsTests
{
    [Fact]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_Ok_Is_Returned()
    {
        using var environment = GetTrackRouteTestEnvironment.ForExistingTrack();

        var response = await environment.Client.GetAsync("/tracks/" + global::Soundtrail.Services.Tests.TestTrackIds.Value("track-501"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
