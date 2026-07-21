using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupMusicbrainzSearchResults;

public sealed class MusicbrainzCatalogSearchPortTests
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

        var action = () => subject.ReadAsync(new SearchCriteria("rare song", SearchType.Track), CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Given_Malformed_Json_When_Reading_Then_An_Exception_Is_Thrown()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ not-json }")
            }));

        var action = () => subject.ReadAsync(new SearchCriteria("rare song", SearchType.Track), CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Given_An_Unexpected_Artist_Response_Contract_When_Reading_Then_An_Invalid_Operation_Exception_Is_Thrown()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"count":1}""")
            }));

        var action = () => subject.ReadAsync(new SearchCriteria("test artist", SearchType.Artist), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("MusicBrainz artist search response must include artists.");
    }

    private static MusicbrainzCatalogSearchPort CreateSubject(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost", UriKind.Absolute)
        };

        return new MusicbrainzCatalogSearchPort(
            client,
            Options.Create(new MusicBrainzOptions
            {
                BaseUrl = "http://localhost",
                UserAgent = "Soundtrail.Tests/1.0"
            }));
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
