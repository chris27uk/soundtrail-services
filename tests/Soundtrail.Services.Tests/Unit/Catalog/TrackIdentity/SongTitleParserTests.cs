using Soundtrail.Domain.Catalog.Tracks.Parsing;

namespace Soundtrail.Services.Tests.Unit.Catalog.TrackIdentity;

public sealed class SongTitleParserTests
{
    [Theory]
    [InlineData("Song (Radio Edit)", "song", "radio edit")]
    [InlineData("Song [Live Mix]", "song", "live mix")]
    [InlineData("Song - Radio Edit", "song", "radio edit")]
    public void Given_A_Title_With_A_Recognised_Trailing_Qualifier_When_Parsing_Then_The_Song_And_Release_Type_Are_Separated(
        string input,
        string expectedSongTitle,
        string expectedReleaseType)
    {
        var parsed = SongTitleParser.Parse(input);
        parsed.Should().BeOfType<SongTitleParseResult.Success>();
        var value = ((SongTitleParseResult.Success)parsed).Value;

        value.CanonicalTrackTitle.Value.Should().Be(expectedSongTitle);
        value.CanonicalReleaseType!.Value.Should().Be(expectedReleaseType);
    }

    [Theory]
    [InlineData("Release Me", "release me")]
    [InlineData("Song - Part II", "song part ii")]
    [InlineData("Instrumental Love", "instrumental love")]
    public void Given_A_Title_Without_A_Recognised_Trailing_Qualifier_When_Parsing_Then_The_Full_Title_Remains_The_Song_Title(
        string input,
        string expectedSongTitle)
    {
        var parsed = SongTitleParser.Parse(input);
        parsed.Should().BeOfType<SongTitleParseResult.Success>();
        var value = ((SongTitleParseResult.Success)parsed).Value;

        value.CanonicalTrackTitle.Value.Should().Be(expectedSongTitle);
        value.CanonicalReleaseType.Should().BeNull();
    }

    [Theory]
    [InlineData(null, SongTitleParseFailure.MissingInput)]
    [InlineData("", SongTitleParseFailure.MissingInput)]
    [InlineData("   ", SongTitleParseFailure.MissingInput)]
    [InlineData("(_", SongTitleParseFailure.MissingCanonicalMeaning)]
    [InlineData("Song (Radio Edit", SongTitleParseFailure.UnclosedReleaseTypeQualifier)]
    [InlineData("Song [Live Mix", SongTitleParseFailure.UnclosedReleaseTypeQualifier)]
    public void Given_Invalid_Or_Malformed_Titles_When_Parsing_Then_A_Failure_Result_Is_Returned(
        string? input,
        SongTitleParseFailure expectedFailure)
    {
        var parsed = SongTitleParser.Parse(input);

        parsed.Should().BeOfType<SongTitleParseResult.Failure>();
        ((SongTitleParseResult.Failure)parsed).Reason.Should().Be(expectedFailure);
    }
}
