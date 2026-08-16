using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Mapping;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Mapping;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzReleaseGroupDumpRowMapperTests
{
    private readonly MusicBrainzReleaseGroupDumpRowMapper mapper = new();

    [Fact]
    public void Given_A_Wrapped_Release_Group_When_Mapped_Then_The_Album_Id_Uses_Credited_Artist_And_Release_Group()
    {
        var line = MusicBrainzReleaseGroupJsonLine.WrapForCreditedArtist(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            """{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}}]}""");

        var album = mapper.TryMap(line);

        album!.AlbumId.StableValue.Should().Be(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa:rg111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void Given_A_Wrapped_Release_Group_When_Mapped_Then_The_Title_Is_Mapped()
    {
        var line = MusicBrainzReleaseGroupJsonLine.WrapForCreditedArtist(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            """{"id":"rg111111-1111-1111-1111-111111111111","title":"Solo Album","artist-credit":[{"artist":{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}}]}""");

        mapper.TryMap(line)!.AlbumTitle.Should().Be("Solo Album");
    }

    [Fact]
    public void Given_A_Bad_Row_When_Mapped_Then_Null_Is_Returned()
    {
        mapper.TryMap("""{"creditedArtistId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"}""").Should().BeNull();
    }
}

public sealed class MusicBrainzReleaseGroupMultiCreditCopyTests
{
    [Fact]
    public void Given_Multiple_Credits_When_Reading_Then_Each_Artist_Id_Is_Returned()
    {
        MusicBrainzReleaseGroupJsonLine.TryReadCreditedArtistIds(
                """{"id":"rg","title":"X","artist-credit":[{"artist":{"id":"a1"}},{"artist":{"id":"a2"}}]}""",
                out var artistIds)
            .Should().BeTrue();

        artistIds.Should().Equal("a1", "a2");
    }

    [Fact]
    public void Given_Multiple_Credits_When_Wrapping_Then_Each_Credited_Artist_Gets_A_Copy()
    {
        const string line = """{"id":"rg","title":"X","artist-credit":[{"artist":{"id":"a1"}},{"artist":{"id":"a2"}}]}""";
        MusicBrainzReleaseGroupJsonLine.TryReadCreditedArtistIds(line, out var artistIds).Should().BeTrue();

        var copies = artistIds
            .Select(artistId => MusicBrainzReleaseGroupJsonLine.WrapForCreditedArtist(artistId, line))
            .ToArray();

        copies.Should().HaveCount(2);
        copies.Should().OnlyContain(copy => copy.Contains("\"releaseGroup\"", StringComparison.Ordinal));
    }
}
