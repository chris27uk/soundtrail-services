using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Scheduled.ImportMusicBrainzDump.Scenarios.NoExistingJob;

public sealed class ImportMusicBrainzDumpTriggerTests
{
    [Fact]
    public async Task Given_A_Monthly_Trigger_When_Handling_Then_A_Job_Is_Ensured()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.JobStore.Jobs.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Monthly_Trigger_When_Handling_Then_The_Job_Id_Matches_The_Dump_Month()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.JobStore.Jobs.Single().Id.Should().Be(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"));
    }

    [Fact]
    public async Task Given_A_Monthly_Trigger_When_Handling_Then_The_Dump_Version_Is_The_Trigger_Month()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.JobStore.Jobs.Single().DumpVersion.Should().Be("2026-08");
    }

    [Fact]
    public async Task Given_A_Monthly_Trigger_When_Handling_Then_The_Job_Is_Pending()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.JobStore.Jobs.Single().Status.Should().Be(MusicBrainzDumpImportJobStatus.Pending);
    }

    [Fact]
    public async Task Given_A_Monthly_Trigger_When_Handling_Then_Start_Is_Published()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();
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

        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-02T00:00:00Z"));

        environment.JobStore.Jobs.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_An_Existing_Pending_Job_When_Retriggered_Then_Start_Is_Published_Again()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.Create();

        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));
        await environment.TriggerAsync(DateTimeOffset.Parse("2026-08-02T00:00:00Z"));

        environment.SentStarts.Should().HaveCount(2);
    }
}
