namespace Soundtrail.Services.Tests.Integration.Ports.ImportKworbChart;

public sealed class MatchingTrackFingerprintDoesNotExistTests
{
    public static TheoryData<LoadTrackByFingerprintPortImplementation> Implementations => new()
    {
        LoadTrackByFingerprintPortImplementation.Fake,
        LoadTrackByFingerprintPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Missing_Track_Fingerprint_When_Loading_The_Track_Id_Then_No_Track_Id_Is_Returned(LoadTrackByFingerprintPortImplementation implementation)
    {
        await using var environment = await LoadTrackByFingerprintPortContractTestEnvironment.ForMissingTrack(implementation);

        var result = await environment.Subject.LoadTrackIdAsync(environment.Fingerprint, CancellationToken.None);

        result.Should().BeNull();
    }
}
