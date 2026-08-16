using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump.Support;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Scheduled.ImportMusicBrainzDump.Scenarios.TracksAvailableInDump.CatalogImport;

public sealed class TracksImportedTests
{
    [Fact]
    public async Task When_Processed_Then_Recordings_Shards_Are_Published()
    {
        using var environment = TracksEnvironment([DumpCatalogRows.TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.SentShards.Count(shard => shard.Phase == MusicBrainzDumpImportPhase.Recordings)
            .Should().Be(2);
    }

    [Fact]
    public async Task When_Processed_Then_Tracks_Are_Imported()
    {
        using var environment = TracksEnvironment([DumpCatalogRows.TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.TrackWriter.Imported.Should().ContainSingle();
    }

    [Fact]
    public async Task When_Processed_Then_Imported_Track_Has_MusicBrainz_Source_Id()
    {
        using var environment = TracksEnvironment([DumpCatalogRows.TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.TrackWriter.Imported.Single().SourceSystemIds
            .Should().Contain(SourceSystemId.MusicBrainz("rec111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task When_Processed_Then_Imported_Track_Title_Comes_From_The_Dump()
    {
        using var environment = TracksEnvironment([DumpCatalogRows.TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.TrackWriter.Imported.Single().Title.Should().Be("Solo Song");
    }

    [Fact]
    public async Task Given_A_Multi_Credit_Track_When_Processed_Then_A_Track_Is_Imported_Per_Credited_Artist()
    {
        using var environment = TracksEnvironment(
            [DumpCatalogRows.TrackMulti],
            [DumpCatalogRows.ReleaseGroupMulti]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.TrackWriter.Imported.Should().HaveCount(2);
    }

    [Fact]
    public async Task When_Processed_Then_The_Job_Is_Completed()
    {
        using var environment = TracksEnvironment([DumpCatalogRows.TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        var job = environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"));
        job.Status.Should().Be(MusicBrainzDumpImportJobStatus.Completed);
    }

    [Fact]
    public async Task When_Processed_Then_The_Job_Progress_Is_Complete()
    {
        using var environment = TracksEnvironment([DumpCatalogRows.TrackSingle]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .ProgressPercent.Should().Be(100);
    }

    [Fact]
    public async Task Given_A_Bad_Track_Row_When_Processed_Then_Only_Valid_Tracks_Are_Imported()
    {
        using var environment = TracksEnvironment([DumpCatalogRows.TrackSingle, DumpCatalogRows.BadTrack]);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.TrackWriter.Imported.Should().ContainSingle();
    }

    private static ImportMusicBrainzDumpSociableTestEnvironment TracksEnvironment(
        IReadOnlyList<string> tracks,
        IReadOnlyList<string>? releaseGroups = null) =>
        ImportMusicBrainzDumpSociableTestEnvironment.ForTracksAvailableInDump(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            [DumpCatalogRows.ArtistA, DumpCatalogRows.ArtistB],
            releaseGroups ?? [DumpCatalogRows.ReleaseGroupSingle],
            tracks);
}
