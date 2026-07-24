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
    public void Given_A_Derived_Track_Id_When_Creating_It_Then_The_Base_Component_Is_Present()
    {
        var trackId = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");

        trackId.BaseComponent.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Given_A_Derived_Track_Id_When_Creating_It_Then_The_Vector_Contains_The_Release_Date()
    {
        var trackId = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");

        trackId.Vector.ReleaseDate.Should().Be(new DateOnly(1997, 5, 21));
    }

    [Fact]
    public void Given_A_Derived_Track_Id_When_Creating_It_Then_The_Vector_Contains_The_Release_Type()
    {
        var trackId = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");

        trackId.Vector.ReleaseTypeDiscriminator.Should().NotBe(0U);
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
    public void Given_The_Same_Base_Metadata_With_Different_Release_Dates_When_Creating_Track_Ids_Then_The_Base_Component_Matches()
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

        older.BaseComponent.Should().Be(newer.BaseComponent);
    }

    [Fact]
    public void Given_The_Same_Artist_And_Track_With_Different_Albums_When_Creating_Track_Ids_Then_The_Base_Component_Matches()
    {
        var albumOne = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var albumTwo = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "Greatest Hits",
            new DateOnly(2008, 1, 1),
            "studio");

        albumOne.BaseComponent.Should().Be(albumTwo.BaseComponent);
    }

    [Fact]
    public void Given_The_Same_Base_Metadata_With_Different_Release_Dates_When_Creating_Track_Ids_Then_They_Share_The_Same_Base_Family()
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

        older.SharesBaseWith(newer).Should().BeTrue();
    }

    [Fact]
    public void Given_The_Same_Base_Metadata_With_Different_Release_Dates_When_Creating_Track_Ids_Then_The_Vector_Differs()
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

        older.Vector.Should().NotBe(newer.Vector);
    }

    [Fact]
    public void Given_A_Track_Id_Value_When_Loading_It_Then_The_Value_Is_Preserved()
    {
        var original = TestTrackIds.Create("track-123");
        var trackId = TrackId.From(original.Value);

        trackId.Value.Should().Be(original.Value);
    }

    [Fact]
    public void Given_A_Track_Id_Value_When_Loading_It_Then_The_Base_Component_Is_Preserved()
    {
        var original = TestTrackIds.Create("track-123");
        var trackId = TrackId.From(original.Value);

        trackId.BaseComponent.Should().Be(original.BaseComponent);
    }

    [Fact]
    public void Given_A_Track_Id_Value_When_Loading_It_Then_The_Vector_Release_Date_Is_Preserved()
    {
        var original = TestTrackIds.Create("track-123");
        var trackId = TrackId.From(original.Value);

        trackId.Vector.ReleaseDate.Should().Be(original.Vector.ReleaseDate);
    }

    [Fact]
    public void Given_A_Track_Id_Value_When_Loading_It_Then_The_Vector_Release_Type_Is_Preserved()
    {
        var original = TestTrackIds.Create("track-123");
        var trackId = TrackId.From(original.Value);

        trackId.Vector.ReleaseTypeDiscriminator.Should().Be(original.Vector.ReleaseTypeDiscriminator);
    }

    [Fact]
    public void Given_A_Derived_Track_Id_When_Creating_It_Then_The_Value_Uses_Fixed_Width_Hex_Encoding()
    {
        var trackId = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");

        trackId.Value.Should().MatchRegex("^trk2_[0-9a-f]{56}$");
    }

    [Fact]
    public void Given_A_Derived_Track_Id_When_Projecting_For_Indexes_Then_The_Base_Is_Exposed_As_Two_Unsigned_Parts()
    {
        var trackId = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");

        var projection = TrackIdIndexProjection.From(trackId);

        projection.BaseHigh.Should().NotBe(0UL);
        projection.BaseLow.Should().NotBe(0UL);
    }

    [Fact]
    public void Given_A_Derived_Track_Id_When_Projecting_For_Indexes_Then_The_Vector_Dimensions_Are_Exposed()
    {
        var trackId = TrackId.Create(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");

        var projection = TrackIdIndexProjection.From(trackId);

        projection.AlbumDiscriminator.Should().NotBe(0U);
        projection.ReleaseDateOrdinal.Should().Be((uint)new DateOnly(1997, 5, 21).DayNumber);
        projection.ReleaseTypeDiscriminator.Should().NotBe(0U);
    }
}
