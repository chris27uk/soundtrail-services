using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpSnapshotIdTests
{
    [Theory]
    [InlineData("20260808-001002")]
    [InlineData("2026-08")]
    public void Given_A_Concrete_Snapshot_Id_When_Parsed_Then_It_Is_Accepted(string value)
    {
        MusicBrainzDumpSnapshotId.Parse(value).Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("LATEST")]
    [InlineData("latest")]
    [InlineData("latest-is-20260808-001002")]
    [InlineData("../escape")]
    [InlineData("a/b")]
    public void Given_An_Invalid_Snapshot_Id_When_Parsed_Then_It_Fails(string? value)
    {
        var act = () => MusicBrainzDumpSnapshotId.Parse(value!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_A_Pointer_Token_When_TryParse_Then_It_Returns_False()
    {
        MusicBrainzDumpSnapshotId.TryParse("LATEST", out _).Should().BeFalse();
    }
}
