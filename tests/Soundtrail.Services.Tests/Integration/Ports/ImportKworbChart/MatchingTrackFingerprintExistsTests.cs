using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Integration.Ports.ImportKworbChart;

public sealed class MatchingTrackFingerprintExistsTests
{
    public static TheoryData<LoadTrackByFingerprintPortImplementation> Implementations => new()
    {
        LoadTrackByFingerprintPortImplementation.Fake,
        LoadTrackByFingerprintPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Matching_Track_Fingerprint_When_Loading_The_Track_Id_Then_A_Track_Id_Is_Returned(LoadTrackByFingerprintPortImplementation implementation)
    {
        await using var environment = await LoadTrackByFingerprintPortContractTestEnvironment.ForExistingTrack(implementation);

        var result = await environment.Subject.LoadTrackIdAsync(environment.Fingerprint, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Matching_Track_Fingerprint_When_Loading_The_Track_Id_Then_The_Track_Id_Is_Returned(LoadTrackByFingerprintPortImplementation implementation)
    {
        await using var environment = await LoadTrackByFingerprintPortContractTestEnvironment.ForExistingTrack(
            implementation,
            trackId: "track-1803");

        var result = await environment.Subject.LoadTrackIdAsync(environment.Fingerprint, CancellationToken.None);

        result.Should().Be(TrackId.From("track-1803"));
    }
}
