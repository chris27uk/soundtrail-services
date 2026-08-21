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

    [Theory]
    [InlineData("20260815-001001", "2026-08-15T00:10:01+00:00")]
    [InlineData("20260808-001002", "2026-08-08T00:10:02+00:00")]
    [InlineData("20260815", "2026-08-15T00:00:00+00:00")]
    [InlineData("2026-08", "2026-08-01T00:00:00+00:00")]
    public void Given_A_Snapshot_Id_When_ToObservedAtUtc_Then_It_Uses_The_Dump_Clock(
        string value,
        string expected)
    {
        MusicBrainzDumpSnapshotId.Parse(value).ToObservedAtUtc()
            .Should().Be(DateTimeOffset.Parse(expected));
    }

    [Fact]
    public void Given_A_Non_Timestamp_Snapshot_Id_When_ToObservedAtUtc_Then_It_Fails()
    {
        var act = () => MusicBrainzDumpSnapshotId.Parse("not-a-date").ToObservedAtUtc();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_A_Pointer_Token_When_TryParse_Then_It_Returns_False()
    {
        MusicBrainzDumpSnapshotId.TryParse("LATEST", out _).Should().BeFalse();
    }
}
