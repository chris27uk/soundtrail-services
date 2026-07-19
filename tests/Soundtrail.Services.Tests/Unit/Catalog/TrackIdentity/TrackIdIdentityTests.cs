using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests.Unit.Catalog.TrackIdentity;

public sealed class TrackIdIdentityTests
{
    [Fact]
    public void Given_The_Same_Canonical_Metadata_When_Creating_A_Track_Id_Then_The_Result_Is_Deterministic()
    {
        var first = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var second = TrackId.Create(
            "  radiohead  ",
            "karma police",
            "ok computer",
            new DateOnly(1997, 5, 21),
            "studio");

        first.Should().Be(second);
    }

    [Fact]
    public void Given_A_Derived_Track_Id_When_Creating_It_Then_The_Base_Key_High_Value_Is_Present()
    {
        var trackId = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");

        trackId.BaseKeyHigh.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Given_A_Derived_Track_Id_When_Creating_It_Then_The_Base_Key_Low_Value_Is_Present()
    {
        var trackId = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");

        trackId.BaseKeyLow.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Given_A_Derived_Track_Id_When_Creating_It_Then_The_Specific_Key_Value_Is_Present()
    {
        var trackId = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");

        trackId.SpecificKey.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Given_A_Derived_Track_Id_When_Creating_It_Then_It_Can_Be_Round_Tripped_From_Its_Value()
    {
        var trackId = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");

        TrackId.From(trackId.Value).Should().Be(trackId);
    }

    [Fact]
    public void Given_The_Same_Base_Metadata_With_Different_Release_Dates_When_Creating_Track_Ids_Then_The_Exact_Ids_Differ()
    {
        var older = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var newer = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(2009, 1, 1),
            "studio");

        older.Value.Should().NotBe(newer.Value);
    }

    [Fact]
    public void Given_The_Same_Base_Metadata_With_Different_Release_Dates_When_Creating_Track_Ids_Then_The_Base_Key_High_Value_Matches()
    {
        var older = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var newer = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(2009, 1, 1),
            "studio");

        older.BaseKeyHigh.Should().Be(newer.BaseKeyHigh);
    }

    [Fact]
    public void Given_The_Same_Base_Metadata_With_Different_Release_Dates_When_Creating_Track_Ids_Then_The_Base_Key_Low_Value_Matches()
    {
        var older = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var newer = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(2009, 1, 1),
            "studio");

        older.BaseKeyLow.Should().Be(newer.BaseKeyLow);
    }

    [Fact]
    public void Given_The_Same_Base_Metadata_With_Different_Release_Dates_When_Creating_Track_Ids_Then_The_Specific_Key_Differs()
    {
        var older = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var newer = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(2009, 1, 1),
            "studio");

        older.SpecificKey.Should().NotBe(newer.SpecificKey);
    }

    [Fact]
    public void Given_A_Track_Id_Value_When_Loading_It_Then_The_Value_Is_Preserved()
    {
        var original = TestTrackIds.Create("track-123");
        var trackId = TrackId.From(original.Value);

        trackId.Value.Should().Be(original.Value);
    }

    [Fact]
    public void Given_A_Track_Id_Value_When_Loading_It_Then_The_Base_Key_High_Value_Is_Preserved()
    {
        var original = TestTrackIds.Create("track-123");
        var trackId = TrackId.From(original.Value);

        trackId.BaseKeyHigh.Should().Be(original.BaseKeyHigh);
    }

    [Fact]
    public void Given_A_Track_Id_Value_When_Loading_It_Then_The_Base_Key_Low_Value_Is_Preserved()
    {
        var original = TestTrackIds.Create("track-123");
        var trackId = TrackId.From(original.Value);

        trackId.BaseKeyLow.Should().Be(original.BaseKeyLow);
    }

    [Fact]
    public void Given_A_Track_Id_Value_When_Loading_It_Then_The_Specific_Key_Value_Is_Preserved()
    {
        var original = TestTrackIds.Create("track-123");
        var trackId = TrackId.From(original.Value);

        trackId.SpecificKey.Should().Be(original.SpecificKey);
    }
}
