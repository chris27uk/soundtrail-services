using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpImportProgressTests
{
    [Theory]
    [InlineData(MusicBrainzDumpImportPhase.Artists, 0.0)]
    [InlineData(MusicBrainzDumpImportPhase.ReleaseGroups, 100.0 / 3.0)]
    [InlineData(MusicBrainzDumpImportPhase.Recordings, 200.0 / 3.0)]
    public void Given_A_Phase_When_Producer_Publishes_Then_Progress_Is_Phase_Midpoint(
        MusicBrainzDumpImportPhase phase,
        double phaseBase)
    {
        MusicBrainzDumpImportProgress.AfterProducerPublished(phase)
            .Should().Be(phaseBase + (MusicBrainzDumpImportProgress.PhaseSpan / 2.0));
    }

    [Theory]
    [InlineData(MusicBrainzDumpImportPhase.Artists, 1, 2)]
    [InlineData(MusicBrainzDumpImportPhase.ReleaseGroups, 2, 4)]
    public void Given_Partial_Shard_Completion_When_Rolling_Up_Then_Progress_Uses_Second_Half_Of_Phase_Band(
        MusicBrainzDumpImportPhase phase,
        int completedShards,
        int totalShards)
    {
        var midpoint = MusicBrainzDumpImportProgress.AfterProducerPublished(phase);
        var expected = midpoint
            + (MusicBrainzDumpImportProgress.PhaseSpan / 2.0)
            * (completedShards / (double)totalShards);

        MusicBrainzDumpImportProgress.AfterShardCompleted(phase, completedShards, totalShards)
            .Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public void Given_All_Artists_Shards_Completed_When_Rolling_Up_Then_Progress_Is_End_Of_Artists_Band()
    {
        MusicBrainzDumpImportProgress.AfterShardCompleted(
                MusicBrainzDumpImportPhase.Artists,
                completedShards: 2,
                totalShardsInPhase: 2)
            .Should().BeApproximately(MusicBrainzDumpImportProgress.PhaseSpan, 1e-9);
    }

    [Fact]
    public void Given_All_Recordings_Shards_Completed_When_Rolling_Up_Then_Progress_Reaches_One_Hundred()
    {
        MusicBrainzDumpImportProgress.AfterShardCompleted(
                MusicBrainzDumpImportPhase.Recordings,
                completedShards: 2,
                totalShardsInPhase: 2)
            .Should().Be(100.0);
    }

    [Fact]
    public void Given_Terminal_Progress_Then_It_Is_One_Hundred()
    {
        MusicBrainzDumpImportProgress.Terminal.Should().Be(100.0);
    }

    [Fact]
    public void Given_Completed_Phase_Shards_When_Counting_Then_Completed_And_Total_Match()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        job.RegisterPhaseShards(MusicBrainzDumpImportPhase.Artists, 3);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0).MarkCompleted();
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 1).MarkCompleted();

        var (completed, total) = MusicBrainzDumpImportProgress.CountPhaseShards(
            job,
            MusicBrainzDumpImportPhase.Artists);

        completed.Should().Be(2);
        total.Should().Be(3);
    }
}
