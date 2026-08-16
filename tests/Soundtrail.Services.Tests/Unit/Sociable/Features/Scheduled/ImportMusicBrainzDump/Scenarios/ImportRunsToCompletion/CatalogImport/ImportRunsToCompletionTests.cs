using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Scheduled.ImportMusicBrainzDump.Scenarios.ImportRunsToCompletion.CatalogImport;

public sealed class ImportRunsToCompletionTests
{
    [Fact]
    public async Task Given_A_Trigger_When_Start_Is_Processed_Then_The_Job_Is_Downloading()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerStartOnlyAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .Status.Should().Be(MusicBrainzDumpImportJobStatus.Downloading);
    }

    [Fact]
    public async Task Given_A_Trigger_When_Start_Is_Processed_Then_The_Producer_Lease_Is_Claimed()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerStartOnlyAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .ProducerLease.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_A_Trigger_When_Start_Is_Processed_Then_The_Producer_Lease_Owner_Is_This_Process()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerStartOnlyAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .ProducerLease!.Owner.Should().Be(CatalogImportLeaseOwnerFake.Default.Value);
    }

    [Fact]
    public async Task Given_A_Trigger_When_Start_Is_Processed_Then_The_Producer_Lease_Is_Active()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerStartOnlyAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .ProducerLease!.IsActive(DateTimeOffset.Parse("2026-08-01T00:01:00Z"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Given_A_Trigger_When_Processed_Through_CatalogImport_Then_The_Job_Is_Completed()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .Status.Should().Be(MusicBrainzDumpImportJobStatus.Completed);
    }

    [Fact]
    public async Task Given_A_Trigger_When_Processed_Through_CatalogImport_Then_Artists_Shards_Are_Published()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.SentShards.Count(shard => shard.Phase == MusicBrainzDumpImportPhase.Artists).Should().Be(2);
    }

    [Fact]
    public async Task Given_A_Completed_Shard_When_The_Shard_Message_Is_Redelivered_Then_The_Shard_Stays_Completed()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await environment.TriggerAndProcessAsync(triggeredAt);

        await environment.EnqueueShardAndProcessAsync(
            jobId,
            MusicBrainzDumpImportPhase.Artists,
            shardId: 0,
            requestedAt: triggeredAt.AddMinutes(1));

        environment.RequireJob(jobId)
            .GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0)
            .Status.Should().Be(MusicBrainzDumpImportShardStatus.Completed);
    }

    [Fact]
    public async Task Given_A_Completed_Shard_When_The_Shard_Message_Is_Redelivered_Then_The_Shard_Lease_Is_Cleared()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await environment.TriggerAndProcessAsync(triggeredAt);

        await environment.EnqueueShardAndProcessAsync(
            jobId,
            MusicBrainzDumpImportPhase.Artists,
            shardId: 0,
            requestedAt: triggeredAt.AddMinutes(1));

        environment.RequireJob(jobId)
            .GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0)
            .Lease.Should().BeNull();
    }

    [Fact]
    public async Task Given_Current_Phase_Is_Artists_When_A_Recordings_Shard_Arrives_Before_Producer_Then_It_Is_Not_Claimed()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await environment.TriggerStartOnlyAsync(triggeredAt);
        await environment.EnqueueShardAndProcessShardHandlersOnlyAsync(
            jobId,
            MusicBrainzDumpImportPhase.Recordings,
            shardId: 0,
            requestedAt: triggeredAt);

        environment.RequireJob(jobId)
            .Shards.Any(shard => shard.Phase == MusicBrainzDumpImportPhase.Recordings)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Given_Current_Phase_Is_Artists_When_A_Recordings_Shard_Arrives_Before_Producer_Then_The_Job_Stays_Downloading()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await environment.TriggerStartOnlyAsync(triggeredAt);
        await environment.EnqueueShardAndProcessShardHandlersOnlyAsync(
            jobId,
            MusicBrainzDumpImportPhase.Recordings,
            shardId: 0,
            requestedAt: triggeredAt);

        environment.RequireJob(jobId).Status.Should().Be(MusicBrainzDumpImportJobStatus.Downloading);
    }
}
