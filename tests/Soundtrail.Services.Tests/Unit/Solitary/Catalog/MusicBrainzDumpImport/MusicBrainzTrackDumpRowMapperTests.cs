using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;
using DomainSourceSystemId = Soundtrail.Domain.Catalog.SourceSystemId;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzTrackDumpRowMapperTests
{
    private readonly MusicBrainzTrackDumpRowMapper mapper = new();

    private const string SoloTrack = """{"id":"rec111111-1111-1111-1111-111111111111","title":"Solo Song","length":210000,"artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","name":"Artist A"}}],"release-group":{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album"},"release-date":"2020-05-01"}""";

    [Fact]
    public void Given_A_Wrapped_Track_When_Mapped_Then_The_Title_Is_Mapped()
    {
        var line = MusicBrainzTrackJsonLine.WrapForCreditedArtist(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            SoloTrack);

        mapper.TryMap(line)!.Title.Should().Be("Solo Song");
    }

    [Fact]
    public void Given_A_Wrapped_Track_When_Mapped_Then_The_Album_Id_Uses_Credited_Artist_And_Release_Group()
    {
        var line = MusicBrainzTrackJsonLine.WrapForCreditedArtist(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            SoloTrack);

        mapper.TryMap(line)!.AlbumId.Should().Be(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa:rg111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void Given_A_Wrapped_Track_When_Mapped_Then_The_Recording_Source_Id_Is_Set()
    {
        var line = MusicBrainzTrackJsonLine.WrapForCreditedArtist(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            SoloTrack);

        mapper.TryMap(line)!.SourceSystemIds
            .Should().Contain(DomainSourceSystemId.MusicBrainz("rec111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public void Given_A_Wrapped_Track_When_Mapped_Then_The_Release_Date_Is_Mapped()
    {
        var line = MusicBrainzTrackJsonLine.WrapForCreditedArtist(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            SoloTrack);

        mapper.TryMap(line)!.ReleaseDate.Should().Be(new DateOnly(2020, 5, 1));
    }

    [Fact]
    public void Given_A_Bad_Row_When_Mapped_Then_Null_Is_Returned()
    {
        mapper.TryMap("""{"creditedArtistId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}""").Should().BeNull();
    }
}

public sealed class MusicBrainzTrackMultiCreditCopyTests
{
    [Fact]
    public void Given_Multiple_Credits_When_Reading_Then_Each_Artist_Id_Is_Returned()
    {
        MusicBrainzTrackJsonLine.TryReadCreditedArtistIds(
                """{"id":"rec","title":"X","artist-credit":[{"artist":{"id":"a1"}},{"artist":{"id":"a2"}}],"release-group":{"id":"rg","title":"A"}}""",
                out var artistIds)
            .Should().BeTrue();

        artistIds.Should().Equal("a1", "a2");
    }

    [Fact]
    public void Given_Multiple_Credits_When_Wrapping_Then_Each_Credited_Artist_Gets_A_Copy()
    {
        const string line = """{"id":"rec","title":"X","artist-credit":[{"artist":{"id":"a1"}},{"artist":{"id":"a2"}}],"release-group":{"id":"rg","title":"A"}}""";
        MusicBrainzTrackJsonLine.TryReadCreditedArtistIds(line, out var artistIds).Should().BeTrue();

        var copies = artistIds
            .Select(artistId => MusicBrainzTrackJsonLine.WrapForCreditedArtist(artistId, line))
            .ToArray();

        copies.Should().HaveCount(2);
        copies.Should().OnlyContain(copy => copy.Contains("\"track\"", StringComparison.Ordinal));
    }
}
