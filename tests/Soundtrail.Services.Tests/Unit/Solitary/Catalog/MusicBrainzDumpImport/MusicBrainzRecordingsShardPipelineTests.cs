using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Mapping;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzRecordingsShardPipelineTests
{
    private const string SoloRelease = """
        {"id":"rel1","title":"Solo Album","date":"2020-05-01","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}],"release-group":{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album"},"media":[{"position":1,"format":"Digital Media","tracks":[{"id":"trk1","position":1,"title":"Solo Song","length":210000,"recording":{"id":"rec111111-1111-1111-1111-111111111111","title":"Solo Song","length":210000,"artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}]}}]}]}
        """;

    private const string SecondRelease = """
        {"id":"rel2","title":"Duo Album","date":"2021-05-01","artist-credit":[{"artist":{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Artist B"}}],"release-group":{"id":"rg222222-2222-2222-2222-222222222222","title":"Duo Album"},"media":[{"position":1,"format":"Digital Media","tracks":[{"id":"trk2","position":1,"title":"Duo Song","length":180000,"recording":{"id":"rec222222-2222-2222-2222-222222222222","title":"Duo Song","length":180000,"artist-credit":[{"artist":{"id":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","name":"Artist B"}}]}}]}]}
        """;

    [Fact]
    public async Task Given_Release_Lines_When_Processed_In_Parallel_Then_All_Shard_Lines_Are_Emitted()
    {
        var shards = new MusicBrainzDumpShardStoreFake();
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");
        await using var writer = shards.OpenWriter(jobId, MusicBrainzDumpImportPhase.Recordings, shardCount: 4);
        IArtistShardPartitioner partitioner = new ArtistShardPartitioner();

        var progress = await MusicBrainzRecordingsShardPipeline.ProcessReleaseGraphAsync(
            ToAsync([SoloRelease, SecondRelease]),
            skipReleaseLines: 0,
            joinDegreeOfParallelism: 2,
            shardCount: 4,
            partitioner,
            writer,
            onProgress: null,
            CancellationToken.None);

        progress.InputLinesCompleted.Should().Be(2);
        progress.TrackRows.Should().Be(2);
        progress.CopiedRows.Should().Be(2);

        var allLines = shards.Shards.Values.SelectMany(static lines => lines).ToArray();
        allLines.Should().HaveCount(2);
        allLines.Should().Contain(static line => line.Contains("Solo Song", StringComparison.Ordinal));
        allLines.Should().Contain(static line => line.Contains("Duo Song", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Given_A_Checkpoint_When_Resumed_Then_Earlier_Release_Lines_Are_Skipped()
    {
        var shards = new MusicBrainzDumpShardStoreFake();
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");
        await using var writer = shards.OpenWriter(jobId, MusicBrainzDumpImportPhase.Recordings, shardCount: 4);
        IArtistShardPartitioner partitioner = new ArtistShardPartitioner();

        var progress = await MusicBrainzRecordingsShardPipeline.ProcessReleaseGraphAsync(
            ToAsync([SoloRelease, SecondRelease]),
            skipReleaseLines: 1,
            joinDegreeOfParallelism: 2,
            shardCount: 4,
            partitioner,
            writer,
            onProgress: null,
            CancellationToken.None);

        progress.InputLinesCompleted.Should().Be(2);
        progress.TrackRows.Should().Be(1);
        var allLines = shards.Shards.Values.SelectMany(static lines => lines).ToArray();
        allLines.Should().ContainSingle()
            .Which.Should().Contain("Duo Song");
    }

    [Fact]
    public void Given_A_Partial_Trailing_Line_When_Truncated_Then_Only_Complete_Lines_Remain()
    {
        using var directory = TemporaryDirectory.Create();
        var path = Path.Combine(directory.Path, "0.jsonl");
        File.WriteAllText(path, "{\"ok\":true}\n{\"partial\":");

        FileMusicBrainzDumpShardWriter.TruncateTrailingPartialLine(path);

        File.ReadAllText(path).Should().Be("{\"ok\":true}\n");
    }

    [Fact]
    public void Given_Recordings_Checkpoint_When_Set_Then_It_Round_Trips_On_The_Job()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        job.SetRecordingsProducerInputLinesCompleted(42);
        job.RecordingsProducerInputLinesCompleted.Should().Be(42);
        job.ClearRecordingsProducerCheckpoint();
        job.RecordingsProducerInputLinesCompleted.Should().Be(0);
    }

    private static async IAsyncEnumerable<string> ToAsync(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            yield return line;
            await Task.Yield();
        }
    }
}
