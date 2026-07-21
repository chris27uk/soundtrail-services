using System.Net;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupMusicbrainzBrowse;

public sealed class MusicbrainzBrowseFailureTests
{
    [Fact]
    public async Task Given_A_Connectivity_Failure_When_Reading_Then_The_Exception_Is_Propagated()
    {
        using var environment = ReadMusicbrainzBrowsePortContractTestEnvironment.ForConnectivityFailure();

        var action = () => environment.AlbumsPort.ReadAsync(environment.ArtistId, CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Given_Malformed_Json_When_Reading_Then_An_Exception_Is_Thrown()
    {
        using var environment = ReadMusicbrainzBrowsePortContractTestEnvironment.ForMalformedJson();

        var action = () => environment.ArtistTracksPort.ReadAsync(environment.ArtistId, CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Given_An_Unexpected_Response_Contract_When_Reading_Then_An_Invalid_Operation_Exception_Is_Thrown()
    {
        using var environment = ReadMusicbrainzBrowsePortContractTestEnvironment.ForUnexpectedContract();

        var action = () => environment.AlbumsPort.ReadAsync(environment.ArtistId, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("MusicBrainz release browse response must include releases.");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Given_A_Non_Success_Status_Code_When_Reading_Then_An_Http_Request_Exception_Is_Thrown(HttpStatusCode statusCode)
    {
        using var environment = ReadMusicbrainzBrowsePortContractTestEnvironment.ForHttpStatusCode(statusCode);

        var action = () => environment.AlbumTracksPort.ReadAsync(environment.AlbumId, CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }
}
