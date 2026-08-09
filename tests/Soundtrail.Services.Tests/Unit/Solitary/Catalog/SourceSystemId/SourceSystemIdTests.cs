using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.SourceSystemId;

public sealed class SourceSystemIdTests
{
    [Fact]
    public void Given_A_MusicBrainz_Mbid_When_Creating_Then_Stable_Value_Uses_System_Prefix()
    {
        var id = Domain.Catalog.SourceSystemId.MusicBrainz("abc-123");

        id.System.Should().Be("musicbrainz");
        id.Id.Should().Be("abc-123");
        id.StableValue.Should().Be("musicbrainz:abc-123");
    }

    [Fact]
    public void Given_A_Stable_Value_When_Parsing_Then_It_Round_Trips()
    {
        var parsed = Domain.Catalog.SourceSystemId.Parse("spotify:track:xyz");

        parsed.System.Should().Be("spotify");
        parsed.Id.Should().Be("track:xyz");
        parsed.StableValue.Should().Be("spotify:track:xyz");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nocolon")]
    [InlineData(":missing-system")]
    [InlineData("missing-id:")]
    public void Given_An_Invalid_Value_When_Parsing_Then_It_Throws(string? value)
    {
        var act = () => Domain.Catalog.SourceSystemId.Parse(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("nocolon", false)]
    [InlineData("musicbrainz:ok", true)]
    public void Given_A_Value_When_TryParse_Then_It_Reports_Success(string? value, bool expected)
    {
        var success = Domain.Catalog.SourceSystemId.TryParse(value, out var id);

        success.Should().Be(expected);
        if (expected)
        {
            id.StableValue.Should().Be(value);
        }
    }

    [Fact]
    public void Given_A_System_Containing_Colon_When_Constructing_Then_It_Throws()
    {
        var act = () => new Domain.Catalog.SourceSystemId("bad:system", "id");

        act.Should().Throw<ArgumentException>().WithParameterName("system");
    }
}
