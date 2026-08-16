using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Mapping;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class ArtistShardPartitionerTests
{
    [Fact]
    public void Given_The_Same_Artist_Key_When_Partitioned_Then_The_Shard_Is_Stable()
    {
        var partitioner = new ArtistShardPartitioner();

        partitioner.ShardIdFor("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", 8)
            .Should().Be(partitioner.ShardIdFor("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", 8));
    }

    [Fact]
    public void Given_A_Shard_Count_When_Partitioned_Then_The_Shard_Is_In_Range()
    {
        var partitioner = new ArtistShardPartitioner();

        partitioner.ShardIdFor("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", 4)
            .Should().BeInRange(0, 3);
    }
}

public sealed class MusicBrainzArtistDumpRowMapperTests
{
    private readonly MusicBrainzArtistDumpRowMapper mapper = new();

    [Fact]
    public void Given_A_Valid_Artist_Row_When_Mapped_Then_The_Artist_Id_Is_The_Mbid()
    {
        var artist = mapper.TryMap("""{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}""");

        artist!.Id.Value.Should().Be("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    }

    [Fact]
    public void Given_A_Valid_Artist_Row_When_Mapped_Then_The_Name_Is_Mapped()
    {
        var artist = mapper.TryMap("""{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}""");

        artist!.Name.Value.Should().Be("Artist A");
    }

    [Fact]
    public void Given_A_Valid_Artist_Row_When_Mapped_Then_The_MusicBrainz_Source_Id_Is_Set()
    {
        var artist = mapper.TryMap("""{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}""");

        artist!.SourceSystemIds.Should().ContainSingle()
            .Which.StableValue.Should().Be("musicbrainz:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    }

    [Fact]
    public void Given_A_Bad_Row_When_Mapped_Then_Null_Is_Returned()
    {
        mapper.TryMap("""{"name":"Missing Id"}""").Should().BeNull();
    }

    [Fact]
    public void Given_Invalid_Json_When_Mapped_Then_Null_Is_Returned()
    {
        mapper.TryMap("{not-json").Should().BeNull();
    }
}

public sealed class MusicBrainzDumpImportJobPhaseTests
{
    [Fact]
    public void Given_All_Artists_Shards_Completed_When_Advancing_Then_Current_Phase_Is_ReleaseGroups()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.Artists, 2);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0).MarkCompleted();
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 1).MarkCompleted();

        job.TryAdvancePhase().Should().BeTrue();
        job.CurrentPhase.Should().Be(MusicBrainzDumpImportPhase.ReleaseGroups);
    }

    [Fact]
    public void Given_All_ReleaseGroups_Shards_Completed_When_Advancing_Then_Current_Phase_Is_Recordings()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.Artists, 1);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0).MarkCompleted();
        job.TryAdvancePhase().Should().BeTrue();
        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.ReleaseGroups, 2);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.ReleaseGroups, 0).MarkCompleted();
        job.GetOrAddShard(MusicBrainzDumpImportPhase.ReleaseGroups, 1).MarkCompleted();

        job.TryAdvancePhase().Should().BeTrue();
        job.CurrentPhase.Should().Be(MusicBrainzDumpImportPhase.Recordings);
    }

    [Fact]
    public void Given_All_Recordings_Shards_Completed_When_Completing_Then_The_Job_Is_Completed()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.Artists, 1);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0).MarkCompleted();
        job.TryAdvancePhase().Should().BeTrue();
        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.ReleaseGroups, 1);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.ReleaseGroups, 0).MarkCompleted();
        job.TryAdvancePhase().Should().BeTrue();
        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.Recordings, 2);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Recordings, 0).MarkCompleted();
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Recordings, 1).MarkCompleted();

        job.TryCompleteRecordingsPhaseAsFinal(DateTimeOffset.Parse("2026-08-01T02:00:00Z")).Should().BeTrue();
        job.Status.Should().Be(MusicBrainzDumpImportJobStatus.Completed);
    }

    [Fact]
    public void Given_Incomplete_Recordings_Shards_When_Completing_Then_It_Fails()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.Artists, 1);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0).MarkCompleted();
        job.TryAdvancePhase().Should().BeTrue();
        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.ReleaseGroups, 1);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.ReleaseGroups, 0).MarkCompleted();
        job.TryAdvancePhase().Should().BeTrue();
        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.Recordings, 2);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Recordings, 0).MarkCompleted();

        job.TryCompleteRecordingsPhaseAsFinal(DateTimeOffset.Parse("2026-08-01T02:00:00Z")).Should().BeFalse();
        job.Status.Should().Be(MusicBrainzDumpImportJobStatus.Pending);
    }
}
