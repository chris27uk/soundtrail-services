using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupStreamingLocation;

public sealed class StreamingLocationDoesNotExistTests
{
    public static TheoryData<ReadStreamingLocationByProviderPortImplementation> Implementations => new()
    {
        ReadStreamingLocationByProviderPortImplementation.Fake,
        ReadStreamingLocationByProviderPortImplementation.WireMock
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Missing_Provider_Link_When_Requesting_By_Isrc_Then_Null_Is_Returned(
        ReadStreamingLocationByProviderPortImplementation implementation)
    {
        using var environment = ReadStreamingLocationByProviderPortContractTestEnvironment.ForMissingProviderLink(implementation);

        var result = await environment.Subject.ReadByIsrcAsync("GBAYE2409901", ProviderName.Spotify, CancellationToken.None);

        result.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Missing_Provider_Link_When_Requesting_By_Metadata_Then_Null_Is_Returned(
        ReadStreamingLocationByProviderPortImplementation implementation)
    {
        using var environment = ReadStreamingLocationByProviderPortContractTestEnvironment.ForMissingProviderLink(implementation);

        var result = await environment.Subject.ReadByTrackMetadataAsync(
            "Northbound",
            "Summer Lights",
            ProviderName.AppleMusic,
            CancellationToken.None);

        result.Should().BeNull();
    }
}
