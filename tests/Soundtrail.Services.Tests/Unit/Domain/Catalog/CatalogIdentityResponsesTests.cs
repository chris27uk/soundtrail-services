using FluentAssertions;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Unit.Domain.Catalog;

public sealed class CatalogIdentityResponsesTests
{
    [Fact]
    public void Given_An_Empty_Artist_Id_When_Creating_It_Then_It_Is_Rejected()
    {
        var act = () => ArtistId.From(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_An_Empty_Album_Id_When_Creating_It_Then_It_Is_Rejected()
    {
        var act = () => AlbumId.From("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_An_Empty_Track_Id_When_Creating_It_Then_It_Is_Rejected()
    {
        var act = () => TrackId.From("");

        act.Should().Throw<ArgumentException>();
    }
}
