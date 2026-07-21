using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.StreamingLocations;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupStreamingLocations;

public sealed class OdesliStreamingLocationPortTests
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Given_A_Non_Success_Status_Code_When_Reading_Then_An_Http_Request_Exception_Is_Thrown(HttpStatusCode statusCode)
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(string.Empty)
            }));

        var action = () => subject.ReadByIsrcAsync("GBAYE2409901", ProviderName.Spotify, CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Given_A_Connectivity_Failure_When_Reading_Then_The_Exception_Is_Propagated()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => throw new HttpRequestException("Connection refused.")));

        var action = () => subject.ReadByTrackMetadataAsync("Northbound", "Summer Lights", ProviderName.AppleMusic, CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>().WithMessage("Connection refused.");
    }

    [Fact]
    public async Task Given_Malformed_Json_When_Reading_Then_An_Exception_Is_Thrown()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ not-json }")
            }));

        var action = () => subject.ReadByIsrcAsync("GBAYE2409901", ProviderName.Spotify, CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Given_An_Unexpected_Response_Contract_When_Reading_Then_An_Invalid_Operation_Exception_Is_Thrown()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"pageUrl":"https://song.link/example"}""")
            }));

        var action = () => subject.ReadByIsrcAsync("GBAYE2409901", ProviderName.Spotify, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Odesli response must include linksByPlatform.");
    }

    [Fact]
    public async Task Given_The_Requested_Provider_Is_Missing_When_Reading_Then_Null_Is_Returned()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"linksByPlatform":{"appleMusic":{"url":"https://music.apple.com/track/abc"}}}""")
            }));

        var result = await subject.ReadByIsrcAsync("GBAYE2409901", ProviderName.Spotify, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_An_Invalid_Provider_Url_When_Reading_Then_An_Invalid_Operation_Exception_Is_Thrown()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"linksByPlatform":{"spotify":{"url":"not-a-url"}}}""")
            }));

        var action = () => subject.ReadByIsrcAsync("GBAYE2409901", ProviderName.Spotify, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Odesli provider link must be an absolute URL.");
    }

    private static OdesliStreamingLocationPort CreateSubject(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost", UriKind.Absolute)
        };

        return new OdesliStreamingLocationPort(
            client,
            Options.Create(new OdesliOptions
            {
                BaseUrl = "http://localhost",
                UserCountry = "US"
            }));
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
