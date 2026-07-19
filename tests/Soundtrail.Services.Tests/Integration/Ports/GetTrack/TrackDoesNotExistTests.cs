using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTrack;

public sealed class TrackDoesNotExistTests
{
    public static TheoryData<GetTrackPortImplementation> Implementations => new()
    {
        GetTrackPortImplementation.Fake,
        GetTrackPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Missing_Track_When_Requesting_The_Track_Then_No_Track_Is_Returned(GetTrackPortImplementation implementation)
    {
        await using var environment = await GetTrackPortContractTestEnvironment.ForMissingTrack(
            implementation,
            global::Soundtrail.Services.Tests.TestTrackIds.Create("track-609"));

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result.Should().BeNull();
    }
}
