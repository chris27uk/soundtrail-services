using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupMusicbrainzBrowse;

public sealed class MusicbrainzCatalogBrowsePortTests
{
    [Fact]
    public async Task Given_An_Artist_Tracks_Response_With_Invalid_Titles_When_Reading_Then_Invalid_Tracks_Are_Skipped()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "recordings": [
                        {
                          "id": "recording-1",
                          "title": "Valid Song [Live Mix]",
                          "first-release-date": "2024-03-04",
                          "releases": [{ "id": "release-artist-track-1", "title": "Live Sessions", "date": "2024-03-04" }],
                          "artist-credit": [{ "name": "The Artist" }]
                        },
                        {
                          "id": "recording-2",
                          "title": "(_",
                          "artist-credit": [{ "name": "The Artist" }]
                        }
                      ]
                    }
                    """)
            }));

        var result = await ((IReadTracksByArtistIdPort)subject).ReadAsync(ArtistId.From("artist-1"), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Select(x => x.Item).Should().ContainSingle(item => item is Soundtrail.Domain.Catalog.CatalogItem.MusicTrack);
        var track = ((CatalogItem.MusicTrack)result.Single().Item).Track;
        track.AlbumId.Should().Be("artist-1:release-artist-track-1");
        track.AlbumTitle.Should().Be("Live Sessions");
        track.ReleaseDate.Should().Be(new DateOnly(2024, 3, 4));
        track.ReleaseType.Should().Be("live mix");
    }

    [Fact]
    public async Task Given_An_Album_Tracks_Response_With_Invalid_Titles_When_Reading_Then_Invalid_Tracks_Are_Skipped()
    {
        var subject = CreateSubject(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "title": "The Album",
                      "date": "2024-01-02",
                      "artist-credit": [{ "name": "The Artist" }],
                      "media": [
                        {
                          "tracks": [
                            { "title": "Valid Song - Radio Edit", "recording": { "id": "recording-1", "isrcs": [] } },
                            { "title": "(_", "recording": { "id": "recording-2", "isrcs": [] } }
                          ]
                        }
                      ]
                    }
                    """)
            }));

        var result = await ((IReadTracksByAlbumIdPort)subject).ReadAsync(AlbumId.From("artist-1", "album-1"), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Select(x => x.Item).Should().ContainSingle(item => item is Soundtrail.Domain.Catalog.CatalogItem.MusicTrack);
        var track = ((CatalogItem.MusicTrack)result.Single().Item).Track;
        track.AlbumTitle.Should().Be("The Album");
        track.ReleaseDate.Should().Be(new DateOnly(2024, 1, 2));
        track.ReleaseType.Should().Be("radio edit");
    }

    private static MusicbrainzCatalogBrowsePort CreateSubject(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost", UriKind.Absolute)
        };

        return new MusicbrainzCatalogBrowsePort(
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
