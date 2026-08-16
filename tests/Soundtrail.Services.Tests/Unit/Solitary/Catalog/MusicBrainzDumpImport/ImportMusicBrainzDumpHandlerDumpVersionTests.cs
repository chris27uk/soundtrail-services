using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class ImportMusicBrainzDumpHandlerDumpVersionTests
{
    [Fact]
    public void Given_Configured_Dump_Version_When_Resolved_Then_Override_Wins()
    {
        ImportMusicBrainzDumpHandler.ResolveDumpVersion(
                DateTimeOffset.Parse("2027-01-15T12:00:00Z"),
                "2026-08")
            .Should().Be("2026-08");
    }

    [Fact]
    public void Given_No_Configured_Dump_Version_When_Resolved_Then_Triggered_At_Month_Is_Used()
    {
        ImportMusicBrainzDumpHandler.ResolveDumpVersion(
                DateTimeOffset.Parse("2027-01-15T12:00:00Z"),
                null)
            .Should().Be("2027-01");
    }

    [Fact]
    public void Given_Blank_Configured_Dump_Version_When_Resolved_Then_Triggered_At_Month_Is_Used()
    {
        ImportMusicBrainzDumpHandler.ResolveDumpVersion(
                DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
                "  ")
            .Should().Be("2026-08");
    }
}
