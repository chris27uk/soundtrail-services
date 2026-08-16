using System.Text.Json;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Mapping;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Mapping;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzReleaseGraphTrackJoinerTests
{
    private const string SoloRelease = """
        {"id":"rel1","title":"Solo Album","date":"2020-05-01","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}],"release-group":{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album"},"media":[{"position":1,"format":"Digital Media","tracks":[{"id":"trk1","position":1,"title":"Solo Song","length":210000,"recording":{"id":"rec111111-1111-1111-1111-111111111111","title":"Solo Song","length":210000,"artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}]}}]}]}
        """;

    [Fact]
    public void Given_A_Release_When_Joined_Then_A_Denormalized_Track_Line_Is_Emitted()
    {
        var lines = MusicBrainzReleaseGraphTrackJoiner.JoinReleaseLines([SoloRelease]);

        lines.Should().ContainSingle();
        using var document = JsonDocument.Parse(lines[0]);
        document.RootElement.GetProperty("id").GetString().Should().Be("rec111111-1111-1111-1111-111111111111");
        document.RootElement.GetProperty("title").GetString().Should().Be("Solo Song");
        document.RootElement.GetProperty("release-date").GetString().Should().Be("2020-05-01");
        document.RootElement.GetProperty("release-group").GetProperty("title").GetString().Should().Be("Solo Album");
    }

    [Fact]
    public void Given_A_Joined_Track_When_Wrapped_Then_The_Mapper_Accepts_It()
    {
        var joined = MusicBrainzReleaseGraphTrackJoiner.JoinReleaseLines([SoloRelease]).Single();
        var wrapped = MusicBrainzTrackJsonLine.WrapForCreditedArtist(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            joined);

        new MusicBrainzTrackDumpRowMapper().TryMap(wrapped)!.Title.Should().Be("Solo Song");
    }

    [Fact]
    public void Given_Missing_Recording_When_Joined_Then_The_Track_Is_Skipped()
    {
        const string release = """
            {"id":"rel1","date":"2020-01-01","release-group":{"id":"rg1","title":"A"},"media":[{"tracks":[{"id":"trk1","title":"No Recording"}]}]}
            """;

        MusicBrainzReleaseGraphTrackJoiner.JoinReleaseLines([release]).Should().BeEmpty();
    }

    [Fact]
    public void Given_Duplicate_Recording_And_Release_Group_When_Joined_Then_Earliest_Date_Wins()
    {
        const string later = """
            {"id":"rel-later","date":"2021-01-01","release-group":{"id":"rg1","title":"Album"},"media":[{"tracks":[{"recording":{"id":"rec1","title":"Song","artist-credit":[{"artist":{"id":"a1","name":"A"}}],"length":1000}}]}]}
            """;
        const string earlier = """
            {"id":"rel-earlier","date":"2020-01-01","release-group":{"id":"rg1","title":"Album"},"media":[{"tracks":[{"recording":{"id":"rec1","title":"Song","artist-credit":[{"artist":{"id":"a1","name":"A"}}],"length":1000}}]}]}
            """;

        var lines = MusicBrainzReleaseGraphTrackJoiner.JoinReleaseLines([later, earlier]);

        lines.Should().ContainSingle();
        using var document = JsonDocument.Parse(lines[0]);
        document.RootElement.GetProperty("release-date").GetString().Should().Be("2020-01-01");
    }

    [Fact]
    public void Given_Recording_Without_Credits_When_Joined_Then_Release_Credits_Are_Used()
    {
        const string release = """
            {"id":"rel1","date":"2020-05-01","artist-credit":[{"artist":{"id":"a1","name":"Artist A"}}],"release-group":{"id":"rg1","title":"Album"},"media":[{"tracks":[{"recording":{"id":"rec1","title":"Song","length":1000}}]}]}
            """;

        var lines = MusicBrainzReleaseGraphTrackJoiner.JoinReleaseLines([release]);

        lines.Should().ContainSingle();
        using var document = JsonDocument.Parse(lines[0]);
        document.RootElement.GetProperty("artist-credit")[0].GetProperty("artist").GetProperty("id")
            .GetString().Should().Be("a1");
    }
}
