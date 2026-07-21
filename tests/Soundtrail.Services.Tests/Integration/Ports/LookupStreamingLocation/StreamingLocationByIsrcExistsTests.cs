using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupStreamingLocation;

public sealed class StreamingLocationByIsrcExistsTests
{
    public static TheoryData<ReadStreamingLocationByProviderPortImplementation> Implementations => new()
    {
        ReadStreamingLocationByProviderPortImplementation.Fake,
        ReadStreamingLocationByProviderPortImplementation.WireMock
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Isrc_Lookup_When_Requesting_The_Streaming_Location_Then_A_Link_Is_Returned(
        ReadStreamingLocationByProviderPortImplementation implementation)
    {
        using var environment = ReadStreamingLocationByProviderPortContractTestEnvironment.ForExistingLink(implementation);

        var result = await environment.Subject.ReadByIsrcAsync("GBAYE2409901", ProviderName.Spotify, CancellationToken.None);

        result.Should().Be(new Uri("https://open.spotify.com/track/stream-1901", UriKind.Absolute));
    }
}
