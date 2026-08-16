using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpBlobKeysTests
{
    [Fact]
    public void Given_Dump_Version_And_Entity_When_Building_Archive_Key_Then_It_Is_Version_Scoped()
    {
        MusicBrainzDumpBlobKeys.Archive("2026-08", "artist")
            .Should().Be("2026-08/artist.tar.xz");
    }

    [Fact]
    public void Given_Job_Phase_And_Shard_When_Building_Shard_Key_Then_Colons_Are_Safe()
    {
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        MusicBrainzDumpBlobKeys.Shard(jobId, MusicBrainzDumpImportPhase.Artists, 2)
            .Should().Be("musicbrainz-dump_2026-08/Artists/2.jsonl");
    }
}

public sealed class BlobMusicBrainzDumpArchiveStoreTests
{
    [Fact]
    public async Task Given_Archive_Already_On_Blob_When_Ensuring_Artists_Then_Http_Is_Not_Used()
    {
        using var directory = TemporaryDirectory.Create();
        var dumpVersion = "2026-08";
        var blobs = new InMemoryMusicBrainzDumpBlobContainer();
        var archiveKey = MusicBrainzDumpBlobKeys.Archive(dumpVersion, "artist");
        await blobs.UploadFromFileAsync(
            archiveKey,
            MusicBrainzDumpArchiveFixtures.CopyTo(directory.Path, "artist.tar.xz"));

        var downloader = new RecordingDownloader([]);
        var store = CreateStore(directory.Path, blobs, downloader, source: "http");

        var path = await store.EnsureArtistsJsonlAsync(
            MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion),
            dumpVersion);

        downloader.RequestedUrls.Should().BeEmpty();
        File.ReadAllText(path).Should().Contain("Artist A");
        blobs.BlobNames.Should().Contain(archiveKey);
    }

    [Fact]
    public async Task Given_Local_Seed_Archive_When_Ensuring_Artists_Then_It_Is_Uploaded_To_Blob()
    {
        using var directory = TemporaryDirectory.Create();
        var dumpVersion = "2026-08";
        var versionRoot = Path.Combine(directory.Path, dumpVersion);
        Directory.CreateDirectory(versionRoot);
        MusicBrainzDumpArchiveFixtures.CopyTo(versionRoot, "artist.tar.xz");

        var blobs = new InMemoryMusicBrainzDumpBlobContainer();
        var downloader = new RecordingDownloader([]);
        var store = CreateStore(directory.Path, blobs, downloader, source: "fixture");

        var path = await store.EnsureArtistsJsonlAsync(
            MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion),
            dumpVersion);

        downloader.RequestedUrls.Should().BeEmpty();
        File.ReadAllText(path).Should().Contain("Artist A");
        (await blobs.ExistsAsync(MusicBrainzDumpBlobKeys.Archive(dumpVersion, "artist")))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Given_Http_Source_And_Missing_Archive_When_Ensuring_Artists_Then_Download_Is_Uploaded()
    {
        using var directory = TemporaryDirectory.Create();
        var dumpVersion = "2026-08";
        Directory.CreateDirectory(Path.Combine(directory.Path, dumpVersion));

        var blobs = new InMemoryMusicBrainzDumpBlobContainer();
        var downloader = new RecordingDownloader(MusicBrainzDumpArchiveFixtures.ReadBytes("artist.tar.xz"));
        var store = CreateStore(directory.Path, blobs, downloader, source: "http");

        var path = await store.EnsureArtistsJsonlAsync(
            MusicBrainzDumpImportJobId.ForDumpVersion(dumpVersion),
            dumpVersion);

        downloader.RequestedUrls.Should().ContainSingle()
            .Which.Should().EndWith("/2026-08/artist.tar.xz");
        File.ReadAllText(path).Should().Contain("Artist A");
        (await blobs.ExistsAsync(MusicBrainzDumpBlobKeys.Archive(dumpVersion, "artist")))
            .Should().BeTrue();
    }

    private static BlobMusicBrainzDumpArchiveStore CreateStore(
        string archiveDirectory,
        IMusicBrainzDumpBlobContainer blobs,
        IMusicBrainzDumpDownloader downloader,
        string source) =>
        new(
            Options.Create(
                new MusicBrainzDumpOptions
                {
                    Source = source,
                    Storage = MusicBrainzDumpOptions.BlobStorage,
                    ArchiveDirectory = archiveDirectory,
                    BaseUrl = "https://example.test/json-dumps"
                }),
            blobs,
            downloader,
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

public sealed class BlobMusicBrainzDumpShardStoreTests
{
    [Fact]
    public async Task Given_Written_Shard_When_Reading_With_Skip_Then_Remaining_Lines_Are_Returned()
    {
        var blobs = new InMemoryMusicBrainzDumpBlobContainer();
        var store = new BlobMusicBrainzDumpShardStore(blobs);
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await store.WriteShardAsync(
            jobId,
            MusicBrainzDumpImportPhase.Artists,
            shardId: 0,
            ["a", "b", "c"]);

        var lines = new List<string>();
        await foreach (var line in store.ReadShardLinesAsync(
                           jobId,
                           MusicBrainzDumpImportPhase.Artists,
                           shardId: 0,
                           skipLines: 1))
        {
            lines.Add(line);
        }

        lines.Should().Equal("b", "c");
    }
}
