using System.Net;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupMusicbrainzBrowse;

internal sealed class ReadMusicbrainzBrowsePortContractTestEnvironment : IDisposable
{
    private readonly WireMockServer? wireMockServer;
    private readonly HttpClient? httpClient;

    private ReadMusicbrainzBrowsePortContractTestEnvironment(
        IReadAlbumsByArtistIdPort albumsPort,
        IReadTracksByArtistIdPort artistTracksPort,
        IReadTracksByAlbumIdPort albumTracksPort,
        WireMockServer? wireMockServer = null,
        HttpClient? httpClient = null)
    {
        AlbumsPort = albumsPort;
        ArtistTracksPort = artistTracksPort;
        AlbumTracksPort = albumTracksPort;
        this.wireMockServer = wireMockServer;
        this.httpClient = httpClient;
    }

    public IReadAlbumsByArtistIdPort AlbumsPort { get; }
    public IReadTracksByArtistIdPort ArtistTracksPort { get; }
    public IReadTracksByAlbumIdPort AlbumTracksPort { get; }

    public ArtistId ArtistId => ArtistId.From("artist-mb-1");
    public AlbumId AlbumId => AlbumId.From("artist-mb-1", "release-mb-1");

    public static ReadMusicbrainzBrowsePortContractTestEnvironment ForExistingResults(
        ReadMusicbrainzBrowsePortImplementation implementation)
    {
        return implementation switch
        {
            ReadMusicbrainzBrowsePortImplementation.Fake => CreateFakeEnvironment(),
            ReadMusicbrainzBrowsePortImplementation.WireMock => CreateWireMockEnvironment(
                """{"releases":[{"id":"release-mb-1","title":"Rare Release","date":"2026-01-02"}]}""",
                """{"recordings":[{"id":"mbid-rare-1","title":"Rare Unknown Song","length":123000,"first-release-date":"2026-01-02","isrcs":["isrc-rare-1"],"artist-credit":[{"name":"Test Artist"}]}]}""",
                """{"title":"Rare Release","date":"2026-01-02","artist-credit":[{"name":"Test Artist"}],"media":[{"tracks":[{"title":"Album Track 1","length":123000,"artist-credit":[{"name":"Test Artist"}],"recording":{"id":"recording-mb-1","isrcs":["isrc-album-1"]}}]}]}"""),
            _ => throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null)
        };
    }

    public static ReadMusicbrainzBrowsePortContractTestEnvironment ForMalformedJson() =>
        CreateWireMockEnvironment("{ not-json }", "{ not-json }", "{ not-json }");

    public static ReadMusicbrainzBrowsePortContractTestEnvironment ForUnexpectedContract() =>
        CreateWireMockEnvironment("""{"count":1}""", """{"count":1}""", """{"title":"Rare Release"}""");

    public static ReadMusicbrainzBrowsePortContractTestEnvironment ForHttpStatusCode(HttpStatusCode statusCode) =>
        CreateWireMockEnvironment(string.Empty, string.Empty, string.Empty, statusCode);

    public static ReadMusicbrainzBrowsePortContractTestEnvironment ForConnectivityFailure()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:1", UriKind.Absolute),
            Timeout = TimeSpan.FromMilliseconds(250)
        };

        var port = new MusicbrainzCatalogBrowsePort(
            client,
            Options.Create(new Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata.MusicBrainzOptions
            {
                BaseUrl = "http://127.0.0.1:1",
                UserAgent = "Soundtrail.Tests/1.0"
            }));

        return new ReadMusicbrainzBrowsePortContractTestEnvironment(port, port, port, httpClient: client);
    }

    public void Dispose()
    {
        httpClient?.Dispose();
        wireMockServer?.Dispose();
    }

    private static ReadMusicbrainzBrowsePortContractTestEnvironment CreateFakeEnvironment()
    {
        var port = new FakePort();
        return new ReadMusicbrainzBrowsePortContractTestEnvironment(port, port, port);
    }

    private static ReadMusicbrainzBrowsePortContractTestEnvironment CreateWireMockEnvironment(
        string releaseBrowseJson,
        string recordingBrowseJson,
        string releaseLookupJson,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var server = WireMockServer.Start();
        server.Given(Request.Create().WithPath("/ws/2/release").UsingGet())
            .RespondWith(Response.Create().WithStatusCode((int)statusCode).WithHeader("Content-Type", "application/json").WithBody(releaseBrowseJson));
        server.Given(Request.Create().WithPath("/ws/2/recording").UsingGet())
            .RespondWith(Response.Create().WithStatusCode((int)statusCode).WithHeader("Content-Type", "application/json").WithBody(recordingBrowseJson));
        server.Given(Request.Create().WithPath("/ws/2/release/release-mb-1").UsingGet())
            .RespondWith(Response.Create().WithStatusCode((int)statusCode).WithHeader("Content-Type", "application/json").WithBody(releaseLookupJson));

        var client = new HttpClient
        {
            BaseAddress = new Uri(server.Url!, UriKind.Absolute)
        };

        var port = new MusicbrainzCatalogBrowsePort(
            client,
            Options.Create(new Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata.MusicBrainzOptions
            {
                BaseUrl = server.Url!,
                UserAgent = "Soundtrail.Tests/1.0"
            }));

        return new ReadMusicbrainzBrowsePortContractTestEnvironment(port, port, port, server, client);
    }

    private sealed class FakePort : IReadAlbumsByArtistIdPort, IReadTracksByArtistIdPort, IReadTracksByAlbumIdPort
    {
        public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(ArtistId artistId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<CatalogDiscoveryEntry>>(
            [
                new CatalogDiscoveryEntry(
                    artistId,
                    new CatalogItem.MusicAlbum(
                        new Album(
                            AlbumId.From(artistId.Value, "release-mb-1"),
                            "Rare Release",
                            "release-mb-1",
                            new DateOnly(2026, 1, 2),
                            artworkUrl: null,
                            updatedAt: new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero))))
            ]);
        }

        Task<IReadOnlyList<CatalogDiscoveryEntry>> IReadTracksByArtistIdPort.ReadAsync(ArtistId artistId, CancellationToken cancellationToken)
        {
            var track = new Track(TestTrackIds.Create("mb-browse-fake-artist-track"))
            {
                Title = "Rare Unknown Song",
                ArtistName = "Test Artist",
                Isrc = "isrc-rare-1",
                UpdatedAt = new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero)
            };

            return Task.FromResult<IReadOnlyList<CatalogDiscoveryEntry>>([new CatalogDiscoveryEntry(artistId, new CatalogItem.MusicTrack(track))]);
        }

        Task<IReadOnlyList<CatalogDiscoveryEntry>> IReadTracksByAlbumIdPort.ReadAsync(AlbumId albumId, CancellationToken cancellationToken)
        {
            var track = new Track(TrackId.Create("Test Artist", "Album Track 1", "Rare Release", new DateOnly(2026, 1, 2)))
            {
                Title = "Album Track 1",
                ArtistName = "Test Artist",
                AlbumId = albumId.StableValue,
                AlbumTitle = "Rare Release",
                Isrc = "isrc-album-1",
                UpdatedAt = new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero)
            };

            return Task.FromResult<IReadOnlyList<CatalogDiscoveryEntry>>([new CatalogDiscoveryEntry(ArtistId.From(albumId.ArtistId), new CatalogItem.MusicTrack(track))]);
        }
    }
}

public enum ReadMusicbrainzBrowsePortImplementation
{
    Fake,
    WireMock
}
