using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpImportJobTests
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    [Fact]
    public void Given_An_Active_Producer_Lease_When_Another_Owner_Claims_Then_It_Fails()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        var now = DateTimeOffset.Parse("2026-08-01T01:00:00Z");

        job.TryClaimProducer("host-a", now, LeaseDuration).Should().BeTrue();
        job.TryClaimProducer("host-b", now, LeaseDuration).Should().BeFalse();
    }

    [Fact]
    public void Given_An_Expired_Producer_Lease_When_Another_Owner_Claims_Then_It_Succeeds()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        var claimedAt = DateTimeOffset.Parse("2026-08-01T01:00:00Z");

        job.TryClaimProducer("host-a", claimedAt, LeaseDuration).Should().BeTrue();
        job.TryClaimProducer("host-b", claimedAt.Add(LeaseDuration).AddSeconds(1), LeaseDuration)
            .Should().BeTrue();
    }

    [Fact]
    public void Given_A_Completed_Shard_When_Claiming_Again_Then_It_No_Ops()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        var now = DateTimeOffset.Parse("2026-08-01T01:00:00Z");

        job.TryClaimShard(MusicBrainzDumpImportPhase.Artists, 0, "host-a", now, LeaseDuration)
            .Should().BeTrue();
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0).MarkCompleted();

        job.TryClaimShard(MusicBrainzDumpImportPhase.Artists, 0, "host-b", now, LeaseDuration)
            .Should().BeFalse();
    }

    [Fact]
    public void Given_All_Phase_Shards_Completed_When_Advancing_Then_Current_Phase_Moves_Forward()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        var now = DateTimeOffset.Parse("2026-08-01T01:00:00Z");

        job.TryClaimShard(MusicBrainzDumpImportPhase.Artists, 0, "host-a", now, LeaseDuration);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0).MarkCompleted();

        job.TryAdvancePhase().Should().BeTrue();
        job.CurrentPhase.Should().Be(MusicBrainzDumpImportPhase.ReleaseGroups);
    }

    [Fact]
    public void Given_A_Wrong_Phase_When_Claiming_A_Shard_Then_It_Fails()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        job.TryClaimShard(
                MusicBrainzDumpImportPhase.Recordings,
                0,
                "host-a",
                DateTimeOffset.Parse("2026-08-01T01:00:00Z"),
                LeaseDuration)
            .Should().BeFalse();
    }

    [Fact]
    public void Given_A_Terminal_Job_When_Preparing_For_Retrigger_Then_It_Resets_To_Pending()
    {
        var job = MusicBrainzDumpImportJob.CreateNew(
            MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"),
            "2026-08",
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        job.SetStatus(MusicBrainzDumpImportJobStatus.Completed);

        var retriggeredAt = DateTimeOffset.Parse("2026-08-15T12:00:00Z");
        job.PrepareForRetrigger(retriggeredAt);

        job.Status.Should().Be(MusicBrainzDumpImportJobStatus.Pending);
        job.RequestedAt.Should().Be(retriggeredAt);
        job.Shards.Should().BeEmpty();
    }

    [Fact]
    public void Given_Start_And_Shard_Messages_When_Creating_Then_Ids_Are_Deterministic()
    {
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");
        var at = DateTimeOffset.Parse("2026-08-01T00:00:00Z");

        StartMusicBrainzDumpImport.Create(jobId, "2026-08", at).Id.Value
            .Should().Be("mb-dump-start:musicbrainz-dump:2026-08");
        ImportMusicBrainzDumpShard.Create(jobId, MusicBrainzDumpImportPhase.Artists, 3, at).Id.Value
            .Should().Be("mb-dump-shard:musicbrainz-dump:2026-08:Artists:3");
    }
}
