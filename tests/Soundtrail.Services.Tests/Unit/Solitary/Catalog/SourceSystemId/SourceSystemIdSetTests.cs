using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.SourceSystemId;

public sealed class SourceSystemIdSetTests
{
    [Fact]
    public void Given_A_Legacy_Mbid_When_Creating_Then_The_Set_Contains_MusicBrainz()
    {
        var set = SourceSystemIdSet.FromLegacyMusicBrainz("mb-1");

        set.Should().ContainSingle().Which.StableValue.Should().Be("musicbrainz:mb-1");
    }

    [Fact]
    public void Given_A_Blank_Legacy_Mbid_When_Creating_Then_The_Set_Is_Empty()
    {
        SourceSystemIdSet.FromLegacyMusicBrainz("  ").Should().BeEmpty();
        SourceSystemIdSet.FromLegacyMusicBrainz(null).Should().BeEmpty();
    }

    [Fact]
    public void Given_Stable_Values_When_Creating_Then_Invalid_Entries_Are_Skipped()
    {
        var set = SourceSystemIdSet.FromStableValues(["musicbrainz:a", "bad", "spotify:b"]);

        SourceSystemIdSet.ToStableValues(set).Should().Equal("musicbrainz:a", "spotify:b");
    }

    [Fact]
    public void Given_An_Existing_System_When_UnionWith_Then_The_Id_Is_Replaced()
    {
        var target = SourceSystemIdSet.Create(Domain.Catalog.SourceSystemId.MusicBrainz("old"));

        SourceSystemIdSet.UnionWith(
            target,
            [Domain.Catalog.SourceSystemId.MusicBrainz("new"), Domain.Catalog.SourceSystemId.Parse("spotify:x")]);

        SourceSystemIdSet.ToStableValues(target).Should().Equal("musicbrainz:new", "spotify:x");
    }

    [Fact]
    public void Given_Ids_When_Reading_MusicBrainzIdOrNull_Then_It_Returns_The_Mbid()
    {
        var ids = SourceSystemIdSet.Create(
            Domain.Catalog.SourceSystemId.Parse("spotify:x"),
            Domain.Catalog.SourceSystemId.MusicBrainz("mb-9"));

        SourceSystemIdSet.MusicBrainzIdOrNull(ids).Should().Be("mb-9");
        SourceSystemIdSet.MusicBrainzIdOrNull([]).Should().BeNull();
    }
}
