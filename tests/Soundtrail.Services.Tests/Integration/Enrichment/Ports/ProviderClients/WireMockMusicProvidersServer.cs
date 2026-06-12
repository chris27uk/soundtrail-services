using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderClients;

internal sealed class WireMockMusicProvidersServer : IDisposable
{
    private readonly WireMockServer server = WireMockServer.Start();

    public string BaseUrl => server.Urls[0];

    public void SeedMusicBrainz(MusicSearchTerm lookup, SongMetadata metadata)
    {
        lookup.Match(
            (track, artist, album) =>
            {
                server
                    .Given(Request.Create()
                        .UsingGet()
                        .WithPath("/ws/2/recording"))
                    .RespondWith(Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody($$"""
                        {
                          "recordings": [
                            {
                              "id": "{{metadata.Mbid}}",
                              "title": "{{metadata.Title}}",
                              "length": {{metadata.DurationMs?.ToString() ?? "null"}},
                              "score": "100",
                              "isrcs": [],
                              "artist-credit": [
                                { "name": "{{metadata.Artist}}" }
                              ]
                            }
                          ]
                        }
                        """));

                return 0;
            },
            isrc =>
            {
                server
                    .Given(Request.Create()
                        .UsingGet()
                        .WithPath($"/ws/2/isrc/{Uri.EscapeDataString(isrc)}"))
                    .RespondWith(Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody($$"""
                        {
                          "recordings": [
                            {
                              "id": "{{metadata.Mbid}}",
                              "title": "{{metadata.Title}}",
                              "length": {{metadata.DurationMs?.ToString() ?? "null"}},
                              "isrcs": ["{{metadata.Isrc}}"],
                              "artist-credit": [
                                { "name": "{{metadata.Artist}}" }
                              ]
                            }
                          ]
                        }
                        """));

                return 0;
            });
    }

    public void SeedOdesli(MusicSearchTerm searchTerm, IReadOnlyList<ExternalReference> references, string userCountry = "US")
    {
        var youtube = references.SingleOrDefault(x => x.Provider == ProviderName.YoutubeMusic);
        var spotify = references.SingleOrDefault(x => x.Provider == ProviderName.Spotify);
        var apple = references.SingleOrDefault(x => x.Provider == ProviderName.AppleMusic);
        var body = $$"""
        {
          "linksByPlatform": {
            {{(youtube is null ? string.Empty : $@"""youtubeMusic"":{{""url"":""{youtube.Url}""}}")}}
            {{(youtube is not null && (spotify is not null || apple is not null) ? "," : string.Empty)}}
            {{(spotify is null ? string.Empty : $@"""spotify"":{{""url"":""{spotify.Url}""}}")}}
            {{(spotify is not null && apple is not null ? "," : string.Empty)}}
            {{(apple is null ? string.Empty : $@"""appleMusic"":{{""url"":""{apple.Url}""}}")}}
          }
        }
        """;

        server
            .Given(Request.Create()
                .UsingGet()
                .WithPath("/v1-user/links"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    public void Dispose()
    {
        server.Dispose();
    }
}
