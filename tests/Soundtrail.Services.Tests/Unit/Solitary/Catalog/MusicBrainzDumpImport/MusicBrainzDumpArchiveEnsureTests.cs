using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Mapping;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Mapping;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpTarXzExtractorTests
{
    private readonly MusicBrainzDumpTarXzExtractor extractor = new();

    [Fact]
    public void Given_An_Archive_When_Extracted_Then_The_Jsonl_Is_Written()
    {
        using var directory = TemporaryDirectory.Create();
        var archivePath = MusicBrainzDumpArchiveFixtures.CopyTo(directory.Path, "artist.tar.xz");
        var outputPath = Path.Combine(directory.Path, "extracted", "artist.jsonl");

        extractor.EnsureExtracted(archivePath, "artist", outputPath);

        File.ReadAllText(outputPath).Should().Contain("Artist A");
    }

    [Fact]
    public void Given_Extracted_Output_Already_Exists_When_Extracting_Then_It_Is_A_No_Op()
    {
        using var directory = TemporaryDirectory.Create();
        var archivePath = MusicBrainzDumpArchiveFixtures.CopyTo(directory.Path, "artist.tar.xz");
        var outputPath = Path.Combine(directory.Path, "extracted", "artist.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, "existing");

        extractor.EnsureExtracted(archivePath, "artist", outputPath);

        File.ReadAllText(outputPath).Should().Be("existing");
    }

    [Fact]
    public void Given_A_Missing_Member_When_Extracting_Then_It_Fails()
    {
        using var directory = TemporaryDirectory.Create();
        var archivePath = MusicBrainzDumpArchiveFixtures.CopyTo(directory.Path, "artist.tar.xz");
        var outputPath = Path.Combine(directory.Path, "extracted", "artist.jsonl");

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
        var handler = new ResumableHandler("payload");
        var downloader = new HttpMusicBrainzDumpDownloader(new HttpClient(handler));

        await downloader.DownloadAsync("https://example.test/artist.tar.xz", destination);

        handler.RangeFromValues.Should().BeEmpty();
        File.ReadAllText(destination).Should().Be("cached");
    }

    [Fact]
    public async Task Given_Missing_Destination_When_Downloading_Then_The_File_Is_Written()
    {
        using var directory = TemporaryDirectory.Create();
        var destination = Path.Combine(directory.Path, "artist.tar.xz");
        var handler = new ResumableHandler("payload");
        var downloader = new HttpMusicBrainzDumpDownloader(new HttpClient(handler));

        await downloader.DownloadAsync("https://example.test/artist.tar.xz", destination);

        handler.RangeFromValues.Should().ContainSingle().Which.Should().BeNull();
        File.ReadAllText(destination).Should().Be("payload");
        File.Exists(destination + ".partial").Should().BeFalse();
    }

    [Fact]
    public async Task Given_Partial_Exists_When_Downloading_Then_Range_Resume_Completes_The_File()
    {
        using var directory = TemporaryDirectory.Create();
        var destination = Path.Combine(directory.Path, "artist.tar.xz");
        var payload = "0123456789abcdef";
        await File.WriteAllTextAsync(destination + ".partial", payload[..6]);
        var handler = new ResumableHandler(payload);
        var downloader = new HttpMusicBrainzDumpDownloader(new HttpClient(handler));

        await downloader.DownloadAsync("https://example.test/artist.tar.xz", destination);

        handler.RangeFromValues.Should().ContainSingle().Which.Should().Be(6);
        File.ReadAllText(destination).Should().Be(payload);
        File.Exists(destination + ".partial").Should().BeFalse();
    }

    [Fact]
    public async Task Given_Interrupt_Mid_Download_When_Retrying_Then_Partial_Is_Kept_And_Resumed()
    {
        using var directory = TemporaryDirectory.Create();
        var destination = Path.Combine(directory.Path, "artist.tar.xz");
        var payload = Encoding.UTF8.GetBytes("0123456789abcdef");
        var handler = new InterruptThenResumeHandler(payload, failAfterBytes: 7);
        var downloader = new HttpMusicBrainzDumpDownloader(new HttpClient(handler));

        var firstAttempt = () => downloader.DownloadAsync("https://example.test/artist.tar.xz", destination);
        await firstAttempt.Should().ThrowAsync<IOException>();
        File.Exists(destination).Should().BeFalse();
        File.Exists(destination + ".partial").Should().BeTrue();
        new FileInfo(destination + ".partial").Length.Should().Be(7);

        await downloader.DownloadAsync("https://example.test/artist.tar.xz", destination);

        handler.RangeFromValues.Should().Equal(null, 7L);
        File.ReadAllBytes(destination).Should().Equal(payload);
        File.Exists(destination + ".partial").Should().BeFalse();
    }

    [Fact]
    public async Task Given_Server_Ignores_Range_When_Downloading_Then_Partial_Is_Replaced()
    {
        using var directory = TemporaryDirectory.Create();
        var destination = Path.Combine(directory.Path, "artist.tar.xz");
        await File.WriteAllTextAsync(destination + ".partial", "stale");
        var handler = new ResumableHandler("fresh-payload", honorRange: false);
        var downloader = new HttpMusicBrainzDumpDownloader(new HttpClient(handler));

        await downloader.DownloadAsync("https://example.test/artist.tar.xz", destination);

        File.ReadAllText(destination).Should().Be("fresh-payload");
    }

    private sealed class ResumableHandler(string payload, bool honorRange = true) : HttpMessageHandler
    {
        public List<long?> RangeFromValues { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var from = request.Headers.Range?.Ranges.FirstOrDefault()?.From;
            RangeFromValues.Add(from);
            if (honorRange && from is { } start)
            {
                var remainder = payload[(int)start..];
                var response = new HttpResponseMessage(HttpStatusCode.PartialContent)
                {
                    Content = new StringContent(remainder, Encoding.UTF8)
                };
                response.Content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(
                    start,
                    payload.Length - 1,
                    payload.Length);
                response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"dump\"");
                return Task.FromResult(response);
            }

            var full = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8)
            };
            full.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"dump\"");
            return Task.FromResult(full);
        }
    }

    private sealed class InterruptThenResumeHandler(byte[] payload, int failAfterBytes) : HttpMessageHandler
    {
        private int callCount;

        public List<long?> RangeFromValues { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            callCount++;
            var from = request.Headers.Range?.Ranges.FirstOrDefault()?.From;
            RangeFromValues.Add(from);

            if (callCount == 1)
            {
                var failing = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(new ThrowAfterBytesStream(payload, failAfterBytes))
                };
                return Task.FromResult(failing);
            }

            var start = (int)(from ?? 0);
            var remainder = payload.AsMemory(start).ToArray();
            var response = new HttpResponseMessage(HttpStatusCode.PartialContent)
            {
                Content = new ByteArrayContent(remainder)
            };
            response.Content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(
                start,
                payload.Length - 1,
                payload.Length);
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowAfterBytesStream(byte[] bytes, int failAfterBytes) : Stream
    {
        private int position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position >= failAfterBytes)
            {
                throw new IOException("Simulated download interrupt.");
            }

            var available = Math.Min(count, failAfterBytes - position);
            Buffer.BlockCopy(bytes, position, buffer, offset, available);
            position += available;
            return available;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
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
        MusicBrainzDumpArchiveFixtures.CopyTo(versionRoot, "artist.tar.xz");

        var store = CreateStore(directory.Path, source: "local");
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
        Directory.CreateDirectory(Path.Combine(directory.Path, dumpVersion));
        var archiveBytes = MusicBrainzDumpArchiveFixtures.ReadBytes("artist.tar.xz");
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
        MusicBrainzDumpArchiveFixtures.CopyTo(versionRoot, "artist.tar.xz");
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
        MusicBrainzDumpArchiveFixtures.CopyTo(versionRoot, "track.tar.xz");
        var downloader = new RecordingDownloader([]);
        var store = CreateStore(directory.Path, source: "http", downloader);

        var path = await store.EnsureTracksJsonlAsync(
            MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion),
            dumpVersion);

        downloader.RequestedUrls.Should().BeEmpty();
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task Given_Release_Archive_Without_Track_When_Ensuring_Tracks_Then_Joined_Jsonl_Is_Written()
    {
        using var directory = TemporaryDirectory.Create();
        var dumpVersion = "2026-08";
        var versionRoot = Path.Combine(directory.Path, dumpVersion);
        Directory.CreateDirectory(versionRoot);
        MusicBrainzDumpArchiveFixtures.CopyTo(versionRoot, "release.tar.xz");
        var store = CreateStore(directory.Path, source: "local");

        var path = await store.EnsureTracksJsonlAsync(
            MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion),
            dumpVersion);

        var line = (await File.ReadAllLinesAsync(path)).Should().ContainSingle().Subject;
        line.Should().Contain("Solo Song");
        var wrapped = MusicBrainzTrackJsonLine.WrapForCreditedArtist(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            line);
        new MusicBrainzTrackDumpRowMapper().TryMap(wrapped)!.Title.Should().Be("Solo Song");
    }

    [Fact]
    public async Task Given_Http_Source_And_Missing_Track_When_Ensuring_Tracks_Then_Release_May_Be_Downloaded()
    {
        using var directory = TemporaryDirectory.Create();
        var dumpVersion = "2026-08";
        Directory.CreateDirectory(Path.Combine(directory.Path, dumpVersion));
        var downloader = new RecordingDownloader(MusicBrainzDumpArchiveFixtures.ReadBytes("release.tar.xz"));
        var store = CreateStore(directory.Path, source: "http", downloader);

        var path = await store.EnsureTracksJsonlAsync(
            MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion),
            dumpVersion);

        downloader.RequestedUrls.Should().ContainSingle()
            .Which.Should().EndWith("/2026-08/release.tar.xz");
        File.ReadAllText(path).Should().Contain("Solo Song");
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

internal static class MusicBrainzDumpArchiveFixtures
{
    public static string CopyTo(string destinationDirectory, string fileName)
    {
        Directory.CreateDirectory(destinationDirectory);
        var destination = Path.Combine(destinationDirectory, fileName);
        File.Copy(Resolve(fileName), destination, overwrite: true);
        return destination;
    }

    public static byte[] ReadBytes(string fileName) => File.ReadAllBytes(Resolve(fileName));

    private static string Resolve(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Unit", "Solitary", "Catalog", "MusicBrainzDumpImport", "Fixtures", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"MusicBrainz dump fixture '{fileName}' was not found at '{path}'.", path);
        }

        return path;
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
