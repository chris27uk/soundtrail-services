using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using System.Net;

namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Worker.LookupMusicbrainzSearchResults;

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

    [Fact]
    public async Task Given_A_Recording_Search_Response_With_Invalid_Titles_When_Reading_Then_Invalid_Tracks_Are_Skipped()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(request =>
            {
                var body = request.RequestUri!.AbsolutePath switch
                {
                    "/ws/2/artist" => """{"artists": []}""",
                    "/ws/2/release" => """{"releases": []}""",
                    "/ws/2/recording" =>
                        """
                        {
                          "recordings": [
                            {
                              "id": "recording-1",
                              "title": "Valid Song (Radio Edit)",
                              "first-release-date": "2024-06-23",
                              "releases": [{ "id": "release-1", "title": "Radio Singles", "date": "2024-06-23" }],
                              "artist-credit": [{ "name": "The Artist", "artist": { "id": "artist-1" } }]
                            },
                            {
                              "id": "recording-2",
                              "title": "(_",
                              "artist-credit": [{ "name": "The Artist", "artist": { "id": "artist-1" } }]
                            }
                          ]
                        }
                        """,
                    _ => throw new InvalidOperationException($"Unexpected request path '{request.RequestUri!.AbsolutePath}'.")
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body)
                };
            }));

        var result = await subject.ReadAsync(new SearchCriteria("valid song", SearchType.Track), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Select(x => x.Item).Should().ContainSingle(item => item is Soundtrail.Domain.Catalog.CatalogItem.MusicTrack);
        var track = ((CatalogItem.MusicTrack)result.Single().Item).Track;
        track.AlbumId.Should().Be("artist-1:release-1");
        track.AlbumTitle.Should().Be("Radio Singles");
        track.ReleaseDate.Should().Be(new DateOnly(2024, 6, 23));
        track.ReleaseType.Should().Be("radio edit");
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
