using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.TrackIdentity;

public sealed class TrackIdIdentityTests
{
    [Fact]
    public void Given_The_Same_Canonical_Metadata_When_Creating_A_Track_Id_Then_The_Result_Is_Deterministic()
    {
        var first = MustCreate(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var second = MustCreate(
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
        var trackId = MustCreate(
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
        var trackId = MustCreate(
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
        var trackId = MustCreate(
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
        var trackId = MustCreate(
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
        var older = MustCreate(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var newer = MustCreate(
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
        var older = MustCreate(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var newer = MustCreate(
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
        var albumOne = MustCreate(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var albumTwo = MustCreate(
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
        var older = MustCreate(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var newer = MustCreate(
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
        var older = MustCreate(
            "Radiohead",
            "Karma Police",
            "OK Computer",
            new DateOnly(1997, 5, 21),
            "studio");
        var newer = MustCreate(
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
        var trackId = MustCreate(
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
        var trackId = MustCreate(
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
        var trackId = MustCreate(
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

    [Fact]
    public void Given_A_Generic_Request_When_Comparing_Candidates_Then_A_Generic_Candidate_Is_Closer_Than_A_Specific_Version()
    {
        var requested = TrackIdIndexProjection.From(MustCreate("Radiohead", "Karma Police"));
        var genericCandidate = TrackIdIndexProjection.From(MustCreate("Radiohead", "Karma Police"));
        var specificCandidate = TrackIdIndexProjection.From(MustCreate("Radiohead", "Karma Police (Radio Edit)"));

        genericCandidate.GetDistanceTo(requested)
            .Should()
            .BeLessThan(specificCandidate.GetDistanceTo(requested));
    }

    [Fact]
    public void Given_A_Specific_Release_Type_Request_When_Comparing_Candidates_Then_The_Matching_Release_Type_Is_Closer_Than_The_Generic_Candidate()
    {
        var requested = TrackIdIndexProjection.From(MustCreate("Radiohead", "Karma Police (Radio Edit)"));
        var matchingCandidate = TrackIdIndexProjection.From(MustCreate("Radiohead", "Karma Police (Radio Edit)"));
        var genericCandidate = TrackIdIndexProjection.From(MustCreate("Radiohead", "Karma Police"));

        matchingCandidate.GetDistanceTo(requested)
            .Should()
            .BeLessThan(genericCandidate.GetDistanceTo(requested));
    }

    [Fact]
    public void Given_A_Specific_Release_Date_Request_When_Comparing_Candidates_Then_A_Nearer_Date_Is_Closer_Than_A_Distant_Date()
    {
        var requested = TrackIdIndexProjection.From(MustCreate("Radiohead", "Karma Police", releaseDate: new DateOnly(2024, 1, 1)));
        var nearerCandidate = TrackIdIndexProjection.From(MustCreate("Radiohead", "Karma Police", releaseDate: new DateOnly(2022, 1, 1)));
        var distantCandidate = TrackIdIndexProjection.From(MustCreate("Radiohead", "Karma Police", releaseDate: new DateOnly(1970, 1, 1)));

        nearerCandidate.GetDistanceTo(requested)
            .Should()
            .BeLessThan(distantCandidate.GetDistanceTo(requested));
    }

    [Fact]
    public void Given_A_Malformed_Track_Title_When_Trying_To_Create_A_Track_Id_Then_A_Failure_Result_Is_Returned()
    {
        var result = TrackId.TryCreate("Radiohead", "(_");

        result.Should().BeOfType<TrackIdCreateResult.Failure>();
    }

    private static TrackId MustCreate(
        string artistName,
        string trackName,
        string? albumName = null,
        DateOnly? releaseDate = null,
        string? releaseType = null) =>
        TrackId.TryCreate(artistName, trackName, albumName, releaseDate, releaseType) switch
        {
            TrackIdCreateResult.Success success => success.Value,
            TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
            _ => throw new InvalidOperationException("Unexpected TrackId creation result.")
        };
}
