using System.Net;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.StreamingLocations;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupStreamingLocation;

internal sealed class ReadStreamingLocationByProviderPortContractTestEnvironment : IDisposable
{
    private readonly WireMockServer? wireMockServer;
    private readonly HttpClient? httpClient;

    private ReadStreamingLocationByProviderPortContractTestEnvironment(
        IReadStreamingLocationByProviderPort subject,
        WireMockServer? wireMockServer = null,
        HttpClient? httpClient = null)
    {
        Subject = subject;
        this.wireMockServer = wireMockServer;
        this.httpClient = httpClient;
    }

    public IReadStreamingLocationByProviderPort Subject { get; }

    public static ReadStreamingLocationByProviderPortContractTestEnvironment ForExistingLink(
        ReadStreamingLocationByProviderPortImplementation implementation,
        ProviderName? provider = null,
        string? spotifyUrl = "https://open.spotify.com/track/stream-1901",
        string? appleMusicUrl = "https://music.apple.com/track/stream-1901")
    {
        var resolvedProvider = provider ?? ProviderName.Spotify;
        var responseJson = CreateLinksJson(spotifyUrl, appleMusicUrl);

        return implementation switch
        {
            ReadStreamingLocationByProviderPortImplementation.Fake => new ReadStreamingLocationByProviderPortContractTestEnvironment(
                new ReadStreamingLocationByProviderPortFake(
                    resolvedProvider == ProviderName.Spotify
                        ? spotifyUrl
                        : appleMusicUrl)),
            ReadStreamingLocationByProviderPortImplementation.WireMock => CreateWireMockEnvironment(responseJson),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static ReadStreamingLocationByProviderPortContractTestEnvironment ForMissingProviderLink(
        ReadStreamingLocationByProviderPortImplementation implementation) =>
        implementation switch
        {
            ReadStreamingLocationByProviderPortImplementation.Fake => new ReadStreamingLocationByProviderPortContractTestEnvironment(
                new ReadStreamingLocationByProviderPortFake()),
            ReadStreamingLocationByProviderPortImplementation.WireMock => CreateWireMockEnvironment(CreateLinksJson(spotifyUrl: null, appleMusicUrl: null)),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };

    public static ReadStreamingLocationByProviderPortContractTestEnvironment ForMalformedJson() =>
        CreateWireMockEnvironment("{ not-json }");

    public static ReadStreamingLocationByProviderPortContractTestEnvironment ForUnexpectedContract() =>
        CreateWireMockEnvironment("""{"pageUrl":"https://song.link/example"}""");

    public static ReadStreamingLocationByProviderPortContractTestEnvironment ForHttpStatusCode(HttpStatusCode statusCode) =>
        CreateWireMockEnvironment(string.Empty, statusCode);

    public static ReadStreamingLocationByProviderPortContractTestEnvironment ForConnectivityFailure()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:1", UriKind.Absolute),
            Timeout = TimeSpan.FromMilliseconds(250)
        };

        return new ReadStreamingLocationByProviderPortContractTestEnvironment(
            new OdesliStreamingLocationPort(
                client,
                Options.Create(new OdesliOptions
                {
                    BaseUrl = "http://127.0.0.1:1",
                    UserCountry = "US"
                })),
            httpClient: client);
    }

    public void Dispose()
    {
        httpClient?.Dispose();
        wireMockServer?.Dispose();
    }

    private static ReadStreamingLocationByProviderPortContractTestEnvironment CreateWireMockEnvironment(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var server = WireMockServer.Start();
        server
            .Given(Request.Create().WithPath("/v1-user/links").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode((int)statusCode)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(json));

        var client = new HttpClient
        {
            BaseAddress = new Uri(server.Url!, UriKind.Absolute)
        };

        return new ReadStreamingLocationByProviderPortContractTestEnvironment(
            new OdesliStreamingLocationPort(
                client,
                Options.Create(new OdesliOptions
                {
                    BaseUrl = server.Url!,
                    UserCountry = "US"
                })),
            server,
            client);
    }

    private static string CreateLinksJson(string? spotifyUrl, string? appleMusicUrl)
    {
        var platforms = new List<string>();

        if (!string.IsNullOrWhiteSpace(spotifyUrl))
        {
            platforms.Add($"\"spotify\":{{\"url\":\"{spotifyUrl}\"}}");
        }

        if (!string.IsNullOrWhiteSpace(appleMusicUrl))
        {
            platforms.Add($"\"appleMusic\":{{\"url\":\"{appleMusicUrl}\"}}");
        }

        return "{\"linksByPlatform\":{" + string.Join(",", platforms) + "}}";
    }

    private sealed class ReadStreamingLocationByProviderPortFake(string? url = null) : IReadStreamingLocationByProviderPort
    {
        private readonly Uri? uri = string.IsNullOrWhiteSpace(url) ? null : new Uri(url, UriKind.Absolute);

        public Task<Uri?> ReadByIsrcAsync(string isrc, ProviderName provider, CancellationToken cancellationToken) =>
            Task.FromResult(uri);

        public Task<Uri?> ReadByTrackMetadataAsync(string artistName, string trackTitle, ProviderName provider, CancellationToken cancellationToken) =>
            Task.FromResult(uri);
    }
}

public enum ReadStreamingLocationByProviderPortImplementation
{
    Fake,
    WireMock
}
