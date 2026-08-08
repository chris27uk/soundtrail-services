using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests.Integration.GetTrack.Api.Ports;

public sealed class TrackDoesNotExistTests
{
    public static TheoryData<GetTrackPortImplementation> Implementations => new()
    {
        GetTrackPortImplementation.Fake,
        GetTrackPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Missing_Track_When_Requesting_The_Track_Then_No_Track_Is_Returned(
        GetTrackPortImplementation implementation)
    {
        var trackId = TestTrackIds.Create("track-609");
        await using var environment = await GetTrackPortContractTestEnvironment.ForMissingTrack(
            implementation,
            trackId);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result.Should().BeNull();
    }
}
