using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupStreamingLocation;

public sealed class StreamingLocationByTrackMetadataExistsTests
{
    public static TheoryData<ReadStreamingLocationByProviderPortImplementation> Implementations => new()
    {
        ReadStreamingLocationByProviderPortImplementation.Fake,
        ReadStreamingLocationByProviderPortImplementation.WireMock
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_A_Metadata_Lookup_When_Requesting_The_Streaming_Location_Then_A_Link_Is_Returned(
        ReadStreamingLocationByProviderPortImplementation implementation)
    {
        using var environment = ReadStreamingLocationByProviderPortContractTestEnvironment.ForExistingLink(
            implementation,
            provider: ProviderName.AppleMusic);

        var result = await environment.Subject.ReadByTrackMetadataAsync(
            "Northbound",
            "Summer Lights",
            ProviderName.AppleMusic,
            CancellationToken.None);

        result.Should().Be(new Uri("https://music.apple.com/track/stream-1901", UriKind.Absolute));
    }
}
