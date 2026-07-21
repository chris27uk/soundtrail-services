using System.Net;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupPlaylistTracks;

public sealed class PlaylistTracksFailureTests
{
    [Fact]
    public async Task Given_Malformed_Html_When_Reading_Tracks_Then_No_Rows_Are_Returned()
    {
        using var environment = ReadPlaylistTracksByProviderPortContractTestEnvironment.ForMalformedHtml();

        var result = await environment.Subject.ReadAsync(environment.PlaylistId, environment.Provider, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Given_A_Non_Success_Status_Code_When_Reading_Tracks_Then_An_Http_Request_Exception_Is_Thrown(HttpStatusCode statusCode)
    {
        using var environment = ReadPlaylistTracksByProviderPortContractTestEnvironment.ForHttpStatusCode(statusCode);

        var action = () => environment.Subject.ReadAsync(environment.PlaylistId, environment.Provider, CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Given_A_Connectivity_Failure_When_Reading_Tracks_Then_An_Http_Request_Exception_Is_Thrown()
    {
        using var environment = ReadPlaylistTracksByProviderPortContractTestEnvironment.ForConnectivityFailure();

        var action = () => environment.Subject.ReadAsync(environment.PlaylistId, environment.Provider, CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }
}
