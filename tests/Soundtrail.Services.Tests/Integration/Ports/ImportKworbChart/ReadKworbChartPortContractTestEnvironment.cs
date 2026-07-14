using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Adapters;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.Integration.Ports.ImportKworbChart;

internal sealed class ReadKworbChartPortContractTestEnvironment : IDisposable
{
    private readonly WireMockServer? wireMockServer;

    private ReadKworbChartPortContractTestEnvironment(IReadKworbChartPort subject, WireMockServer? wireMockServer = null)
    {
        Subject = subject;
        this.wireMockServer = wireMockServer;
    }

    public IReadKworbChartPort Subject { get; }

    public static ReadKworbChartPortContractTestEnvironment ForExistingChart(ReadKworbChartPortImplementation implementation)
    {
        return implementation switch
        {
            ReadKworbChartPortImplementation.Fake => new ReadKworbChartPortContractTestEnvironment(
                new ReadKworbChartPortFake(
                    new[]
                    {
                        new TrackReference(ArtistName.From("Artist 1901"), "Track 1901")
                    })),
            ReadKworbChartPortImplementation.WireMock => CreateWireMockEnvironment(
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

    public static ReadKworbChartPortContractTestEnvironment ForWorldwideChartFixture() =>
        CreateWireMockEnvironment(ReadFixtureHtml());

    public static ReadKworbChartPortContractTestEnvironment ForEmptyChart(ReadKworbChartPortImplementation implementation) =>
        implementation switch
        {
            ReadKworbChartPortImplementation.Fake => new ReadKworbChartPortContractTestEnvironment(
                new ReadKworbChartPortFake()),
            ReadKworbChartPortImplementation.WireMock => CreateWireMockEnvironment("<html><body><table></table></body></html>"),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };

    public void Dispose()
    {
        wireMockServer?.Dispose();
    }

    private static ReadKworbChartPortContractTestEnvironment CreateWireMockEnvironment(string html)
    {
        var server = WireMockServer.Start();
        server
            .Given(Request.Create().WithPath("/ww/").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(html));

        var client = new HttpClient
        {
            BaseAddress = new Uri(server.Url!, UriKind.Absolute)
        };

        return new ReadKworbChartPortContractTestEnvironment(
            new KworbChartPort(client),
            server);
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

public enum ReadKworbChartPortImplementation
{
    Fake,
    WireMock
}
