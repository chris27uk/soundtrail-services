using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Scheduled.ImportMusicBrainzDump.Scenarios.CatalogImportPipeline;

public sealed class ImportMusicBrainzDumpCatalogImportPipelineTests
{
    [Fact]
    public async Task Given_A_Trigger_When_Processed_Through_CatalogImport_Then_The_Job_Is_Extracting()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .Status.Should().Be(MusicBrainzDumpImportJobStatus.Extracting);
    }

    [Fact]
    public async Task Given_A_Trigger_When_Processed_Through_CatalogImport_Then_The_Producer_Lease_Is_Claimed()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .ProducerLease.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_A_Trigger_When_Processed_Through_CatalogImport_Then_The_Producer_Lease_Owner_Is_This_Process()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .ProducerLease!.Owner.Should().Be(CatalogImportLeaseOwnerFake.Default.Value);
    }

    [Fact]
    public async Task Given_A_Trigger_When_Processed_Through_CatalogImport_Then_The_Producer_Lease_Is_Active()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .ProducerLease!.IsActive(DateTimeOffset.Parse("2026-08-01T00:01:00Z"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Given_A_Claimed_Job_When_A_Shard_Is_Dispatched_Then_The_Job_Is_Importing()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await environment.TriggerAndProcessAsync(triggeredAt);
        await environment.EnqueueShardAndProcessAsync(
            jobId,
            MusicBrainzDumpImportPhase.Artists,
            shardId: 0,
            requestedAt: triggeredAt);

        environment.RequireJob(jobId).Status.Should().Be(MusicBrainzDumpImportJobStatus.Importing);
    }

    [Fact]
    public async Task Given_A_Claimed_Job_When_A_Shard_Is_Dispatched_Then_The_Shard_Is_Leased()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await environment.TriggerAndProcessAsync(triggeredAt);
        await environment.EnqueueShardAndProcessAsync(
            jobId,
            MusicBrainzDumpImportPhase.Artists,
            shardId: 0,
            requestedAt: triggeredAt);

        environment.RequireJob(jobId)
            .GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0)
            .Status.Should().Be(MusicBrainzDumpImportShardStatus.Leased);
    }

    [Fact]
    public async Task Given_A_Claimed_Job_When_A_Shard_Is_Dispatched_Then_The_Shard_Lease_Is_Claimed()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await environment.TriggerAndProcessAsync(triggeredAt);
        await environment.EnqueueShardAndProcessAsync(
            jobId,
            MusicBrainzDumpImportPhase.Artists,
            shardId: 0,
            requestedAt: triggeredAt);

        environment.RequireJob(jobId)
            .GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0)
            .Lease.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_A_Claimed_Job_When_A_Shard_Is_Dispatched_Then_The_Shard_Lease_Owner_Is_This_Process()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await environment.TriggerAndProcessAsync(triggeredAt);
        await environment.EnqueueShardAndProcessAsync(
            jobId,
            MusicBrainzDumpImportPhase.Artists,
            shardId: 0,
            requestedAt: triggeredAt);

        environment.RequireJob(jobId)
            .GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0)
            .Lease!.Owner.Should().Be(CatalogImportLeaseOwnerFake.Default.Value);
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
            requestedAt: triggeredAt);

        var job = environment.RequireJob(jobId);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0).MarkCompleted();
        await environment.JobStore.SaveAsync(job);

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
            requestedAt: triggeredAt);

        var job = environment.RequireJob(jobId);
        job.GetOrAddShard(MusicBrainzDumpImportPhase.Artists, 0).MarkCompleted();
        await environment.JobStore.SaveAsync(job);

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
    public async Task Given_Current_Phase_Is_Artists_When_A_Recordings_Shard_Arrives_Then_No_Shard_Is_Recorded()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await environment.TriggerAndProcessAsync(triggeredAt);
        await environment.EnqueueShardAndProcessAsync(
            jobId,
            MusicBrainzDumpImportPhase.Recordings,
            shardId: 0,
            requestedAt: triggeredAt);

        environment.RequireJob(jobId).Shards.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_Current_Phase_Is_Artists_When_A_Recordings_Shard_Arrives_Then_The_Job_Stays_Extracting()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var jobId = MusicBrainzDumpImportJobId.ForDumpVersion("2026-08");

        await environment.TriggerAndProcessAsync(triggeredAt);
        await environment.EnqueueShardAndProcessAsync(
            jobId,
            MusicBrainzDumpImportPhase.Recordings,
            shardId: 0,
            requestedAt: triggeredAt);

        environment.RequireJob(jobId).Status.Should().Be(MusicBrainzDumpImportJobStatus.Extracting);
    }
}
