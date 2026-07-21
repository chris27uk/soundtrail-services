using System.Net;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupPlaylistTracks;

internal sealed class ReadPlaylistTracksByProviderPortContractTestEnvironment : IDisposable
{
    private readonly WireMockServer? wireMockServer;
    private readonly HttpClient? httpClient;

    private ReadPlaylistTracksByProviderPortContractTestEnvironment(
        IReadPlaylistTracksByProviderPort subject,
        WireMockServer? wireMockServer = null,
        HttpClient? httpClient = null)
    {
        Subject = subject;
        this.wireMockServer = wireMockServer;
        this.httpClient = httpClient;
    }

    public IReadPlaylistTracksByProviderPort Subject { get; }

    public PlaylistId PlaylistId => PlaylistId.FromPlaylistName("WorldwideSongChart");

    public ProviderName Provider => ProviderName.Spotify;

    public static ReadPlaylistTracksByProviderPortContractTestEnvironment ForExistingPlaylist(
        ReadPlaylistTracksByProviderPortImplementation implementation)
    {
        return implementation switch
        {
            ReadPlaylistTracksByProviderPortImplementation.Fake => new ReadPlaylistTracksByProviderPortContractTestEnvironment(
                new ReadPlaylistTracksByProviderPortFake(
                    [
                        new TrackReference(ArtistName.From("Artist 1901"), "Track 1901")
                    ])),
            ReadPlaylistTracksByProviderPortImplementation.WireMock => CreateWireMockEnvironment(
                """
                <html>
                <body>
                    <table>
                        <tbody>
                            <tr>
                                <td>1</td>
                                <td>NEW</td>
                                <td>Artist 1901 - Track 1901</td>
                            </tr>
                        </tbody>
                    </table>
                </body>
                </html>
                """),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static ReadPlaylistTracksByProviderPortContractTestEnvironment ForWorldwideChartFixture() =>
        CreateWireMockEnvironment(ReadFixtureHtml());

    public static ReadPlaylistTracksByProviderPortContractTestEnvironment ForEmptyPlaylist(
        ReadPlaylistTracksByProviderPortImplementation implementation) =>
        implementation switch
        {
            ReadPlaylistTracksByProviderPortImplementation.Fake => new ReadPlaylistTracksByProviderPortContractTestEnvironment(
                new ReadPlaylistTracksByProviderPortFake()),
            ReadPlaylistTracksByProviderPortImplementation.WireMock => CreateWireMockEnvironment("<html><body><table></table></body></html>"),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };

    public static ReadPlaylistTracksByProviderPortContractTestEnvironment ForMalformedHtml() =>
        CreateWireMockEnvironment("<html><body><div>not a chart</div><span>Artist without table</span></body></html>");

    public static ReadPlaylistTracksByProviderPortContractTestEnvironment ForHttpStatusCode(HttpStatusCode statusCode) =>
        CreateWireMockEnvironment(string.Empty, statusCode);

    public static ReadPlaylistTracksByProviderPortContractTestEnvironment ForConnectivityFailure()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:1", UriKind.Absolute),
            Timeout = TimeSpan.FromMilliseconds(250)
        };

        return new ReadPlaylistTracksByProviderPortContractTestEnvironment(
            new KworbPlaylistTracksPort(client),
            httpClient: client);
    }

    public void Dispose()
    {
        httpClient?.Dispose();
        wireMockServer?.Dispose();
    }

    private static ReadPlaylistTracksByProviderPortContractTestEnvironment CreateWireMockEnvironment(
        string html,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var server = WireMockServer.Start();
        server
            .Given(Request.Create().WithPath("/ww/").UsingGet())
            .RespondWith(Response.Create().WithStatusCode((int)statusCode).WithBody(html));

        var client = new HttpClient
        {
            BaseAddress = new Uri(server.Url!, UriKind.Absolute)
        };

        return new ReadPlaylistTracksByProviderPortContractTestEnvironment(
            new KworbPlaylistTracksPort(client),
            server,
            client);
    }

    private static string ReadFixtureHtml()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "Fixtures",
            "kworb-worldwide-song-chart.html");

        return File.ReadAllText(Path.GetFullPath(fixturePath));
    }
}

public enum ReadPlaylistTracksByProviderPortImplementation
{
    Fake,
    WireMock
}
