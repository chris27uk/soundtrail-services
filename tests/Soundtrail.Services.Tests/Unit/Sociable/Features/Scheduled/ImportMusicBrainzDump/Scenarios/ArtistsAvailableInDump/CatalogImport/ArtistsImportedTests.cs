using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump;
using Soundtrail.Services.Tests.Unit.Sociable.Features.ImportMusicBrainzDump.Support;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Scheduled.ImportMusicBrainzDump.Scenarios.ArtistsAvailableInDump.CatalogImport;

public sealed class ArtistsImportedTests
{
    [Fact]
    public async Task When_Processed_Then_Artists_Are_Imported()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.ForArtistsAvailableInDump(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            DumpCatalogRows.ArtistA,
            DumpCatalogRows.ArtistB);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.ArtistWriter.Imported.Should().HaveCount(2);
    }

    [Fact]
    public async Task When_Processed_Then_Imported_Artist_Has_MusicBrainz_Source_Id()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.ForArtistsAvailableInDump(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            DumpCatalogRows.ArtistA);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.ArtistWriter.Imported.Single().SourceSystemIds
            .Should().Contain(SourceSystemId.MusicBrainz("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    }

    [Fact]
    public async Task When_Processed_Then_Imported_Artist_Name_Comes_From_The_Dump()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.ForArtistsAvailableInDump(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            DumpCatalogRows.ArtistA);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.ArtistWriter.Imported.Single().Name.Value.Should().Be("Artist A");
    }

    [Fact]
    public async Task When_Processed_Then_All_Artists_Shards_Are_Completed()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.ForArtistsAvailableInDump(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            DumpCatalogRows.ArtistA,
            DumpCatalogRows.ArtistB);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .Shards.Should().OnlyContain(shard => shard.Status == MusicBrainzDumpImportShardStatus.Completed);
    }

    [Fact]
    public async Task Given_A_Bad_Row_When_Processed_Then_The_Job_Still_Completes()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.ForArtistsAvailableInDump(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            DumpCatalogRows.ArtistA,
            DumpCatalogRows.BadArtist);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.RequireJob(MusicBrainzDumpImportJobId.ForDumpVersion("2026-08"))
            .Status.Should().Be(MusicBrainzDumpImportJobStatus.Completed);
    }

    [Fact]
    public async Task Given_A_Bad_Row_When_Processed_Then_Only_Valid_Artists_Are_Imported()
    {
        using var environment = ImportMusicBrainzDumpSociableTestEnvironment.ForArtistsAvailableInDump(
            DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            DumpCatalogRows.ArtistA,
            DumpCatalogRows.BadArtist);

        await environment.TriggerAndProcessAsync(DateTimeOffset.Parse("2026-08-01T00:00:00Z"));

        environment.ArtistWriter.Imported.Should().ContainSingle();
    }
}
