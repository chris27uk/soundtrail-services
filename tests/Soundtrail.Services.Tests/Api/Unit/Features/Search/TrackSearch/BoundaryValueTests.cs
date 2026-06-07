using FluentAssertions;
using Soundtrail.Services.Api.Features.Search.TrackSearch;

namespace Soundtrail.Services.Tests.Api.Unit.Features.Search.TrackSearch;

public sealed class BoundaryValueTests
{
    [Fact]
    public void Given_An_Omitted_Limit_When_Creating_A_Limit_Then_The_Default_Value_Is_Used()
    {
        var limit = Limit.From(null);

        limit.Value.Should().Be(10);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    public void Given_A_Boundary_Limit_When_Creating_A_Limit_Then_It_Is_Accepted(int value)
    {
        var limit = Limit.From(value);

        limit.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    public void Given_An_Out_Of_Range_Limit_When_Creating_A_Limit_Then_It_Is_Rejected(int value)
    {
        var act = () => Limit.From(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void Given_A_Boundary_ConfidenceScore_When_Creating_It_Then_It_Is_Accepted(double value)
    {
        var confidence = ConfidenceScore.From(value);

        confidence.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Given_An_Out_Of_Range_ConfidenceScore_When_Creating_It_Then_It_Is_Rejected(double value)
    {
        var act = () => ConfidenceScore.From(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
