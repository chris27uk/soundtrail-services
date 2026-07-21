using System.Net;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupStreamingLocation;

public sealed class StreamingLocationFailureTests
{
    [Fact]
    public async Task Given_A_Connectivity_Failure_When_Requesting_The_Streaming_Location_Then_The_Exception_Is_Propagated()
    {
        using var environment = ReadStreamingLocationByProviderPortContractTestEnvironment.ForConnectivityFailure();

        var action = () => environment.Subject.ReadByIsrcAsync("GBAYE2409901", ProviderName.Spotify, CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Given_Malformed_Json_When_Requesting_The_Streaming_Location_Then_An_Exception_Is_Thrown()
    {
        using var environment = ReadStreamingLocationByProviderPortContractTestEnvironment.ForMalformedJson();

        var action = () => environment.Subject.ReadByTrackMetadataAsync("Northbound", "Summer Lights", ProviderName.AppleMusic, CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Given_An_Unexpected_Response_Contract_When_Requesting_The_Streaming_Location_Then_An_Invalid_Operation_Exception_Is_Thrown()
    {
        using var environment = ReadStreamingLocationByProviderPortContractTestEnvironment.ForUnexpectedContract();

        var action = () => environment.Subject.ReadByIsrcAsync("GBAYE2409901", ProviderName.Spotify, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Odesli response must include linksByPlatform.");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Given_A_Non_Success_Status_Code_When_Requesting_The_Streaming_Location_Then_An_Http_Request_Exception_Is_Thrown(
        HttpStatusCode statusCode)
    {
        using var environment = ReadStreamingLocationByProviderPortContractTestEnvironment.ForHttpStatusCode(statusCode);

        var action = () => environment.Subject.ReadByTrackMetadataAsync("Northbound", "Summer Lights", ProviderName.AppleMusic, CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }
}
