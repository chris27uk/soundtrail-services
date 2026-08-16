using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Scheduled.ImportMusicBrainzDump.Scenarios.NoExistingJob;

public sealed class ImportMusicBrainzDumpTriggerTests
{
    [Fact]
    public async Task Given_A_Scheduled_Trigger_When_Handling_Then_A_Job_Is_Ensured_For_Latest_Snapshot()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        environment.SnapshotCatalog.WithLatest("20260808-001002");

        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.JobStore.Jobs.Should().ContainSingle();
        environment.JobStore.Jobs.Single().Id
            .Should().Be(MusicBrainzDumpImportJobId.ForDumpVersion("20260808-001002"));
        environment.JobStore.Jobs.Single().DumpVersion.Should().Be("20260808-001002");
    }

    [Fact]
    public async Task Given_A_Scheduled_Trigger_When_Latest_Advances_Then_A_Distinct_Job_Is_Ensured()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        environment.SnapshotCatalog.WithLatest("20260808-001002");
        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.SnapshotCatalog.WithLatest("20260905-001010");
        await environment.TriggerAsync(DateTimeOffset.Parse("2026-09-01T00:00:00Z"));

        environment.JobStore.Jobs.Should().HaveCount(2);
        environment.JobStore.Jobs.Select(job => job.DumpVersion)
            .Should().BeEquivalentTo("20260808-001002", "20260905-001010");
    }

    [Fact]
    public async Task Given_A_Scheduled_Trigger_When_Handling_Then_The_Job_Is_Pending()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.JobStore.Jobs.Single().Status.Should().Be(MusicBrainzDumpImportJobStatus.Pending);
    }

    [Fact]
    public async Task Given_A_Scheduled_Trigger_When_Handling_Then_Start_Is_Published_For_Latest()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        environment.SnapshotCatalog.WithLatest("20260808-001002");
        var triggeredAt = DateTimeOffset.Parse("2026-08-01T00:00:00Z");

        await environment.TriggerAsync(triggeredAt);

        var job = environment.JobStore.Jobs.Single();
        environment.SentStart.Should().BeEquivalentTo(
            StartMusicBrainzDumpImport.Create(job.Id, job.DumpVersion, triggeredAt));
    }

    [Fact]
    public async Task Given_An_Existing_Pending_Job_When_Retriggered_Then_The_Job_Is_Not_Duplicated()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        environment.SnapshotCatalog.WithLatest("20260808-001002");

        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-02T00:00:00Z"));

        environment.JobStore.Jobs.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_An_Existing_Pending_Job_When_Retriggered_Then_Start_Is_Published_Again()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        environment.SnapshotCatalog.WithLatest("20260808-001002");

        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-02T00:00:00Z"));

        environment.SentStarts.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_A_Manual_Trigger_With_Snapshot_When_Exists_Then_Job_Uses_That_Snapshot()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        environment.SnapshotCatalog.WithExisting("20260701-000001");

        await environment.TriggerManualAsync(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            "20260701-000001");

        environment.JobStore.Jobs.Single().DumpVersion.Should().Be("20260701-000001");
        environment.JobStore.Jobs.Single().Id.Value.Should().Be("musicbrainz-dump:20260701-000001");
    }

    [Fact]
    public async Task Given_A_Manual_Trigger_With_Missing_Snapshot_When_Handling_Then_It_Fails()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        environment.SnapshotCatalog.WithoutExisting("2026-08");

        var act = () => environment.TriggerManualAsync(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            "2026-08");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*was not found*");
        environment.JobStore.Jobs.Should().BeEmpty();
        environment.SentStarts.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Manual_Trigger_Without_Snapshot_When_Handling_Then_It_Fails()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
        var handler = environment.ResolveScheduledHandler();

        var act = () => handler.HandleAsync(
            new ImportMusicBrainzDumpCommand(
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
                Manual: true,
                SnapshotId: null));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires a concrete*");
        environment.JobStore.Jobs.Should().BeEmpty();
        environment.SentStarts.Should().BeEmpty();
    }

    [Fact]
    public void Given_A_Manual_Trigger_With_Latest_Token_When_Parsed_Then_It_Fails()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        var act = () => MusicBrainzDumpSnapshotId.Parse("LATEST");

        act.Should().Throw<ArgumentException>();
        environment.JobStore.Jobs.Should().BeEmpty();
    }
}
