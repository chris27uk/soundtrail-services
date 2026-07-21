using System.Net;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupMusicbrainzSearchResults;

internal sealed class ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment : IDisposable
{
    private readonly WireMockServer? wireMockServer;
    private readonly HttpClient? httpClient;

    private ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment(
        IReadCatalogEntriesBySearchCriteriaPort subject,
        WireMockServer? wireMockServer = null,
        HttpClient? httpClient = null)
    {
        Subject = subject;
        this.wireMockServer = wireMockServer;
        this.httpClient = httpClient;
    }

    public IReadCatalogEntriesBySearchCriteriaPort Subject { get; }

    public static ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment ForExistingResults(
        ReadCatalogEntriesBySearchCriteriaPortImplementation implementation)
    {
        return implementation switch
        {
            ReadCatalogEntriesBySearchCriteriaPortImplementation.Fake =>
                new ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment(
                    new ReadCatalogEntriesBySearchCriteriaPortFake(
                        CreateEntries())),
            ReadCatalogEntriesBySearchCriteriaPortImplementation.WireMock => CreateWireMockEnvironment(
                artistJson: """{"artists":[{"id":"artist-mb-1","name":"Test Artist"}]}""",
                releaseJson: """{"releases":[{"id":"release-mb-1","title":"Rare Release","date":"2026-01-02","artist-credit":[{"name":"Test Artist","artist":{"id":"artist-mb-1"}}]}]}""",
                recordingJson: """{"recordings":[{"id":"mbid-rare-1","title":"Rare Unknown Song","length":123000,"first-release-date":"2026-01-02","isrcs":["isrc-rare-1"],"artist-credit":[{"name":"Test Artist","artist":{"id":"artist-mb-1"}}]}]}"""),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment ForMalformedJson() =>
        CreateWireMockEnvironment("{ not-json }", "{ not-json }", "{ not-json }");

    public static ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment ForUnexpectedContract() =>
        CreateWireMockEnvironment("""{"count":1}""", """{"count":1}""", """{"count":1}""");

    public static ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment ForHttpStatusCode(HttpStatusCode statusCode) =>
        CreateWireMockEnvironment(string.Empty, string.Empty, string.Empty, statusCode);

    public static ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment ForConnectivityFailure()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:1", UriKind.Absolute),
            Timeout = TimeSpan.FromMilliseconds(250)
        };

        return new ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment(
            new MusicbrainzCatalogSearchPort(
                client,
                Options.Create(new MusicBrainzOptions
                {
                    BaseUrl = "http://127.0.0.1:1",
                    UserAgent = "Soundtrail.Tests/1.0"
                })),
            httpClient: client);
    }

    public void Dispose()
    {
        httpClient?.Dispose();
        wireMockServer?.Dispose();
    }

    private static ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment CreateWireMockEnvironment(
        string artistJson,
        string releaseJson,
        string recordingJson,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var server = WireMockServer.Start();
        server.Given(Request.Create().WithPath("/ws/2/artist").UsingGet())
            .RespondWith(Response.Create().WithStatusCode((int)statusCode).WithHeader("Content-Type", "application/json").WithBody(artistJson));
        server.Given(Request.Create().WithPath("/ws/2/release").UsingGet())
            .RespondWith(Response.Create().WithStatusCode((int)statusCode).WithHeader("Content-Type", "application/json").WithBody(releaseJson));
        server.Given(Request.Create().WithPath("/ws/2/recording").UsingGet())
            .RespondWith(Response.Create().WithStatusCode((int)statusCode).WithHeader("Content-Type", "application/json").WithBody(recordingJson));

        var client = new HttpClient
        {
            BaseAddress = new Uri(server.Url!, UriKind.Absolute)
        };

        return new ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment(
            new MusicbrainzCatalogSearchPort(
                client,
                Options.Create(new MusicBrainzOptions
                {
                    BaseUrl = server.Url!,
                    UserAgent = "Soundtrail.Tests/1.0"
                })),
            server,
            client);
    }

    private sealed class ReadCatalogEntriesBySearchCriteriaPortFake(IReadOnlyList<CatalogDiscoveryEntry> entries)
        : IReadCatalogEntriesBySearchCriteriaPort
    {
        public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken) =>
            Task.FromResult(entries);
    }

    private static IReadOnlyList<CatalogDiscoveryEntry> CreateEntries()
    {
        var artistId = ArtistId.From("artist-mb-1");
        var albumId = Soundtrail.Domain.Catalog.Albums.AlbumId.From(artistId.Value, "release-mb-1");
        var trackId = TestTrackIds.Create("musicbrainz-search-track-1");
        var track = new Track(trackId)
        {
            Title = "Rare Unknown Song",
            ArtistName = "Test Artist",
            Isrc = "isrc-rare-1",
            UpdatedAt = new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero)
        };

        return
        [
            new CatalogDiscoveryEntry(
                artistId,
                new CatalogItem.MusicArtist(
                    new Artist
                    {
                        Id = artistId,
                        Name = ArtistName.From("Test Artist")
                    })),
            new CatalogDiscoveryEntry(
                artistId,
                new CatalogItem.MusicAlbum(
                    new Soundtrail.Domain.Catalog.Albums.Album(
                        albumId,
                        "Rare Release",
                        "release-mb-1",
                        new DateOnly(2026, 1, 2),
                        artworkUrl: null,
                        updatedAt: new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero)))),
            new CatalogDiscoveryEntry(
                artistId,
                new CatalogItem.MusicTrack(track))
        ];
    }
}

public enum ReadCatalogEntriesBySearchCriteriaPortImplementation
{
    Fake,
    WireMock
}
