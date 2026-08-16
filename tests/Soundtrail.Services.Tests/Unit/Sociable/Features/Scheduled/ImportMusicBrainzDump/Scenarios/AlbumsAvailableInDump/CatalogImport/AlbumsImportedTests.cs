using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump.Support;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Scheduled.ImportMusicBrainzDump.Scenarios.AlbumsAvailableInDump.CatalogImport;

public sealed class AlbumsImportedTests
{
    [Fact]
    public async Task When_Processed_Then_ReleaseGroups_Shards_Are_Published()
    {
        using var environment = AlbumsEnvironment(DumpCatalogRows.ReleaseGroupSingle);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.SentShards.Count(shard => shard.Phase == MusicBrainzDumpImportPhase.ReleaseGroups)
            .Should().Be(2);
    }

    [Fact]
    public async Task When_Processed_Then_Albums_Are_Imported()
    {
        using var environment = AlbumsEnvironment(DumpCatalogRows.ReleaseGroupSingle);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.AlbumWriter.Imported.Should().ContainSingle();
    }

    [Fact]
    public async Task When_Processed_Then_Imported_Album_Has_MusicBrainz_Source_Id()
    {
        using var environment = AlbumsEnvironment(DumpCatalogRows.ReleaseGroupSingle);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.AlbumWriter.Imported.Single().SourceSystemIds
            .Should().Contain(SourceSystemId.MusicBrainz("rg111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task When_Processed_Then_Imported_Album_Title_Comes_From_The_Dump()
    {
        using var environment = AlbumsEnvironment(DumpCatalogRows.ReleaseGroupSingle);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.AlbumWriter.Imported.Single().AlbumTitle.Should().Be("Solo Album");
    }

    [Fact]
    public async Task Given_A_Multi_Credit_Release_Group_When_Processed_Then_An_Album_Is_Imported_Per_Credited_Artist()
    {
        using var environment = AlbumsEnvironment(DumpCatalogRows.ReleaseGroupMulti);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.AlbumWriter.Imported.Should().HaveCount(2);
    }

    [Fact]
    public async Task When_Processed_Then_The_Job_Is_Completed()
    {
        using var environment = AlbumsEnvironment(DumpCatalogRows.ReleaseGroupSingle);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .Status.Should().Be(MusicBrainzDumpImportJobStatus.Completed);
    }

    [Fact]
    public async Task Given_A_Bad_Release_Group_Row_When_Processed_Then_Only_Valid_Albums_Are_Imported()
    {
        using var environment = AlbumsEnvironment(DumpCatalogRows.ReleaseGroupSingle, DumpCatalogRows.BadReleaseGroup);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.AlbumWriter.Imported.Should().ContainSingle();
    }

    private static ImportMusicBrainzDumpSociableTestEnvironment AlbumsEnvironment(params string[] releaseGroups) =>
        ImportMusicBrainzDumpSociableTestEnvironment.ForAlbumsAvailableInDump(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            [DumpCatalogRows.ArtistA, DumpCatalogRows.ArtistB],
            releaseGroups);
}
