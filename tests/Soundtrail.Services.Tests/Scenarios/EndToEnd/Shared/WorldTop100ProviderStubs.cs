using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.EndToEnd.Shared;

internal static class WorldTop100ProviderStubs
{
    public static void Configure(WireMockServer server)
    {
        ConfigureKworbStub(server);
        ConfigureMusicbrainzStub(server);
        ConfigureOdesliStub(server);
    }

    private static void ConfigureKworbStub(WireMockServer server)
    {
        server.Given(Request.Create().WithPath("/ww/").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody(
                        """
                        <html>
                        <body>
                            <table>
                                <tbody>
                                    <tr><td>1</td><td>NEW</td><td>Aurora Lane - Midnight Signals</td></tr>
                                    <tr><td>2</td><td>+3</td><td>Paper Tigers - Static Hearts</td></tr>
                                    <tr><td>3</td><td>-1</td><td>Neon Harbour - Glass Cities</td></tr>
                                    <tr><td>4</td><td>NEW</td><td>Saturn Kids - Golden Echo</td></tr>
                                </tbody>
                            </table>
                        </body>
                        </html>
                        """));
    }

    private static void ConfigureMusicbrainzStub(WireMockServer server)
    {
        server.Given(Request.Create().WithPath("/ws/2/artist").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"artists":[]}"""));

        server.Given(Request.Create().WithPath("/ws/2/release").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"releases":[]}"""));

        server.Given(
                Request.Create()
                    .WithPath("/ws/2/recording")
                    .WithParam("query", "Midnight Signals Aurora Lane")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """
                        {
                          "recordings": [
                            {
                              "id": "mbid-midnight-signals-original",
                              "title": "Midnight Signals",
                              "length": 214000,
                              "releases": [{ "id": "release-midnight-signals", "title": "Midnight Signals", "date": "2023-11-10" }],
                              "artist-credit": [{ "name": "Aurora Lane", "artist": { "id": "musicbrainz-artist:aurora-lane" } }]
                            }
                          ]
                        }
                        """));

        server.Given(
                Request.Create()
                    .WithPath("/ws/2/recording")
                    .WithParam("query", "Static Hearts Paper Tigers")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """
                        {
                          "recordings": [
                            {
                              "id": "mbid-static-hearts-2022",
                              "title": "Static Hearts",
                              "length": 198000,
                              "first-release-date": "2022-09-16",
                              "releases": [{ "id": "release-static-hearts", "title": "Static Hearts", "date": "2022-09-16" }],
                              "artist-credit": [{ "name": "Paper Tigers", "artist": { "id": "musicbrainz-artist:paper-tigers" } }]
                            }
                          ]
                        }
                        """));

        server.Given(
                Request.Create()
                    .WithPath("/ws/2/recording")
                    .WithParam("query", "Glass Cities Neon Harbour")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """
                        {
                          "recordings": [
                            {
                              "id": "mbid-glass-cities-radio-2024",
                              "title": "Glass Cities (Radio Edit)",
                              "length": 231000,
                              "first-release-date": "2024-06-23",
                              "releases": [{ "id": "release-glass-cities-radio", "title": "Glass Cities Remixes", "date": "2024-06-23" }],
                              "artist-credit": [{ "name": "Neon Harbour", "artist": { "id": "musicbrainz-artist:neon-harbour" } }]
                            }
                          ]
                        }
                        """));

        server.Given(
                Request.Create()
                    .WithPath("/ws/2/recording")
                    .WithParam("query", "Golden Echo Saturn Kids")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """
                        {
                          "recordings": [
                            {
                              "id": "mbid-golden-echo-radio",
                              "title": "Golden Echo - Radio Edit",
                              "length": 244000,
                              "releases": [{ "id": "release-golden-echo-radio", "title": "Golden Echo Radio Release", "date": "2024-02-14" }],
                              "artist-credit": [{ "name": "Saturn Kids", "artist": { "id": "musicbrainz-artist:saturn-kids" } }]
                            }
                          ]
                        }
                        """));
    }

    private static void ConfigureOdesliStub(WireMockServer server)
    {
        server.Given(Request.Create().WithPath("/v1-user/links").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(request =>
                    {
                        var artistName = request.Query?["artistName"]?.FirstOrDefault() ?? string.Empty;
                        var songName = request.Query?["songName"]?.FirstOrDefault() ?? string.Empty;

                        return (artistName, songName) switch
                        {
                            ("Aurora Lane", "Midnight Signals") =>
                                """{"linksByPlatform":{"spotify":{"url":"https://open.spotify.com/track/midnight-signals"}}}""",
                            ("Paper Tigers", "Static Hearts") =>
                                """{"linksByPlatform":{}}""",
                            ("Neon Harbour", "Glass Cities (Radio Edit)") =>
                                """{"linksByPlatform":{"youtubeMusic":{"url":"https://music.youtube.com/watch?v=glass-cities-radio"}}}""",
                            ("Saturn Kids", "Golden Echo - Radio Edit") =>
                                """{"linksByPlatform":{}}""",
                            _ =>
                                "{\"linksByPlatform\":{},\"unmatched\":{\"artistName\":\"" + artistName + "\",\"songName\":\"" + songName + "\"}}"
                        };
                    }));
    }
}
