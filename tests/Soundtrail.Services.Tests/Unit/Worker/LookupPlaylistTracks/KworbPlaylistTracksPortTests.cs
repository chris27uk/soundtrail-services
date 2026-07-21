using System.Net;
using System.Net.Http;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Adapters;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupPlaylistTracks;

public sealed class KworbPlaylistTracksPortTests
{
    [Fact]
    public async Task Given_Malformed_Html_When_Reading_Tracks_Then_No_Rows_Are_Returned()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><body><div>not a chart</div></body></html>")
            }));

        var result = await subject.ReadAsync(
            PlaylistId.FromPlaylistName("WorldwideSongChart"),
            ProviderName.Spotify,
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Given_A_Non_Success_Status_Code_When_Reading_Tracks_Then_An_Http_Request_Exception_Is_Thrown(HttpStatusCode statusCode)
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(string.Empty)
            }));

        var action = () => subject.ReadAsync(
            PlaylistId.FromPlaylistName("WorldwideSongChart"),
            ProviderName.Spotify,
            CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Given_A_Connectivity_Failure_When_Reading_Tracks_Then_The_Exception_Is_Propagated()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => throw new HttpRequestException("Connection refused.")));

        var action = () => subject.ReadAsync(
            PlaylistId.FromPlaylistName("WorldwideSongChart"),
            ProviderName.Spotify,
            CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>().WithMessage("Connection refused.");
    }

    private static KworbPlaylistTracksPort CreateSubject(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost", UriKind.Absolute)
        };

        return new KworbPlaylistTracksPort(client);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
