using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpTarXzExtractorTests
{
    private readonly MusicBrainzDumpTarXzExtractor extractor = new();

    [Fact]
    public void Given_An_Archive_When_Extracted_Then_The_Jsonl_Is_Written()
    {
        using var directory = TemporaryDirectory.Create();
        var archivePath = Path.Combine(directory.Path, "artist.tar.xz");
        var outputPath = Path.Combine(directory.Path, "extracted", "artist.jsonl");
        MusicBrainzDumpTarXzExtractor.CreateFixtureArchive(
            archivePath,
            "artist",
            ["""{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}"""]);

        extractor.EnsureExtracted(archivePath, "artist", outputPath);

        File.ReadAllText(outputPath).Should().Contain("Artist A");
    }

    [Fact]
    public void Given_Extracted_Output_Already_Exists_When_Extracting_Then_It_Is_A_No_Op()
    {
        using var directory = TemporaryDirectory.Create();
        var archivePath = Path.Combine(directory.Path, "artist.tar.xz");
        var outputPath = Path.Combine(directory.Path, "extracted", "artist.jsonl");
        MusicBrainzDumpTarXzExtractor.CreateFixtureArchive(
            archivePath,
            "artist",
            ["""{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}"""]);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, "existing");

        extractor.EnsureExtracted(archivePath, "artist", outputPath);

        File.ReadAllText(outputPath).Should().Be("existing");
    }

    [Fact]
    public void Given_A_Missing_Member_When_Extracting_Then_It_Fails()
    {
        using var directory = TemporaryDirectory.Create();
        var archivePath = Path.Combine(directory.Path, "artist.tar.xz");
        var outputPath = Path.Combine(directory.Path, "extracted", "artist.jsonl");
        MusicBrainzDumpTarXzExtractor.CreateFixtureArchive(
            archivePath,
            "artist",
            ["""{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}"""]);

        var act = () => extractor.EnsureExtracted(archivePath, "release-group", outputPath);

        act.Should().Throw<InvalidOperationException>();
    }
}

public sealed class HttpMusicBrainzDumpDownloaderTests
{
    [Fact]
    public async Task Given_Destination_Already_Exists_When_Downloading_Then_Http_Is_Not_Called()
    {
        using var directory = TemporaryDirectory.Create();
        var destination = Path.Combine(directory.Path, "artist.tar.xz");
        File.WriteAllText(destination, "cached");
        var handler = new CountingHandler();
        var downloader = new HttpMusicBrainzDumpDownloader(new HttpClient(handler));

        await downloader.DownloadAsync("https://example.test/artist.tar.xz", destination);

        handler.CallCount.Should().Be(0);
        File.ReadAllText(destination).Should().Be("cached");
    }

    [Fact]
    public async Task Given_Missing_Destination_When_Downloading_Then_The_File_Is_Written()
    {
        using var directory = TemporaryDirectory.Create();
        var destination = Path.Combine(directory.Path, "artist.tar.xz");
        var handler = new CountingHandler { ResponseBody = "payload" };
        var downloader = new HttpMusicBrainzDumpDownloader(new HttpClient(handler));

        await downloader.DownloadAsync("https://example.test/artist.tar.xz", destination);

        handler.CallCount.Should().Be(1);
        File.ReadAllText(destination).Should().Be("payload");
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public string ResponseBody { get; init; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ResponseBody, Encoding.UTF8)
                });
        }
    }
}

public sealed class LocalMusicBrainzDumpArchiveStoreEnsureTests
{
    [Fact]
    public async Task Given_An_Existing_Archive_When_Ensuring_Artists_Then_Jsonl_Is_Extracted()
    {
        using var directory = TemporaryDirectory.Create();
        var dumpVersion = "2026-08";
        var versionRoot = Path.Combine(directory.Path, dumpVersion);
        Directory.CreateDirectory(versionRoot);
        MusicBrainzDumpTarXzExtractor.CreateFixtureArchive(
            Path.Combine(versionRoot, "artist.tar.xz"),
            "artist",
            ["""{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}"""]);

        var store = CreateStore(directory.Path, source: "fixture");
        var path = await store.EnsureArtistsJsonlAsync(
            MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion),
            dumpVersion);

        File.ReadAllText(path).Should().Contain("Artist A");
    }

    [Fact]
    public async Task Given_Http_Source_And_Missing_Archive_When_Ensuring_Artists_Then_Download_Is_Used()
    {
        using var directory = TemporaryDirectory.Create();
        var dumpVersion = "2026-08";
        var versionRoot = Path.Combine(directory.Path, dumpVersion);
        Directory.CreateDirectory(versionRoot);
        var archiveBytes = CreateArchiveBytes(
            "artist",
            ["""{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}"""]);
        var downloader = new RecordingDownloader(archiveBytes);
        var store = CreateStore(directory.Path, source: "http", downloader);

        var path = await store.EnsureArtistsJsonlAsync(
            MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion),
            dumpVersion);

        downloader.RequestedUrls.Should().ContainSingle()
            .Which.Should().EndWith("/2026-08/artist.tar.xz");
        File.ReadAllText(path).Should().Contain("Artist A");
    }

    [Fact]
    public async Task Given_Http_Source_And_Existing_Archive_When_Ensuring_Artists_Then_Download_Is_Skipped()
    {
        using var directory = TemporaryDirectory.Create();
        var dumpVersion = "2026-08";
        var versionRoot = Path.Combine(directory.Path, dumpVersion);
        Directory.CreateDirectory(versionRoot);
        MusicBrainzDumpTarXzExtractor.CreateFixtureArchive(
            Path.Combine(versionRoot, "artist.tar.xz"),
            "artist",
            ["""{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}"""]);
        var downloader = new RecordingDownloader([]);
        var store = CreateStore(directory.Path, source: "http", downloader);

        var path = await store.EnsureArtistsJsonlAsync(
            MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion),
            dumpVersion);

        downloader.RequestedUrls.Should().BeEmpty();
        File.ReadAllText(path).Should().Contain("Artist A");
    }

    [Fact]
    public async Task Given_Http_Source_When_Ensuring_Tracks_Then_Http_Is_Not_Used()
    {
        using var directory = TemporaryDirectory.Create();
        var dumpVersion = "2026-08";
        var versionRoot = Path.Combine(directory.Path, dumpVersion);
        Directory.CreateDirectory(versionRoot);
        MusicBrainzDumpTarXzExtractor.CreateFixtureArchive(
            Path.Combine(versionRoot, "track.tar.xz"),
            "track",
            ["""{"id":"rec","title":"Song","artist-credit":[{"artist":{"id":"a","name":"A"}}],"release-group":{"id":"rg","title":"Album"}}"""]);
        var downloader = new RecordingDownloader([]);
        var store = CreateStore(directory.Path, source: "http", downloader);

        var path = await store.EnsureTracksJsonlAsync(
            MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion),
            dumpVersion);

        downloader.RequestedUrls.Should().BeEmpty();
        File.Exists(path).Should().BeTrue();
    }

    private static LocalMusicBrainzDumpArchiveStore CreateStore(
        string archiveDirectory,
        string source,
        IMusicBrainzDumpDownloader? downloader = null) =>
        new(
            Options.Create(
                new MusicBrainzDumpOptions
                {
                    Source = source,
                    ArchiveDirectory = archiveDirectory,
                    BaseUrl = "https://example.test/json-dumps"
                }),
            downloader ?? new RecordingDownloader([]),
            new MusicBrainzDumpTarXzExtractor());

    private static byte[] CreateArchiveBytes(string entityName, IEnumerable<string> lines)
    {
        var path = Path.Combine(Path.GetTempPath(), $"mb-archive-{Guid.NewGuid():N}.tar.xz");
        try
        {
            MusicBrainzDumpTarXzExtractor.CreateFixtureArchive(path, entityName, lines);
            return File.ReadAllBytes(path);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class RecordingDownloader(byte[] payload) : IMusicBrainzDumpDownloader
    {
        private readonly List<string> requestedUrls = [];

        public IReadOnlyList<string> RequestedUrls => requestedUrls;

        public async Task DownloadAsync(
            string url,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            requestedUrls.Add(url);
            if (File.Exists(destinationPath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
            await File.WriteAllBytesAsync(destinationPath, payload, cancellationToken);
        }
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    private TemporaryDirectory(string path) => Path = path;

    public string Path { get; }

    public static TemporaryDirectory Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mb-dump-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TemporaryDirectory(path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
