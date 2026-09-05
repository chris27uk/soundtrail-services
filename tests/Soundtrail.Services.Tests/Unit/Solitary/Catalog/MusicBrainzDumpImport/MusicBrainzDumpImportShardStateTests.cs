using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Tests.Unit.Solitary.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpImportShardStateTests
{
    [Fact]
    public void Given_A_Projection_Cursor_When_Advancing_Then_It_Moves_Forward()
    {
        var shard = new MusicBrainzDumpImportShardState(MusicBrainzDumpImportPhase.Artists, 0);
        shard.UpdateProjectionLineOffset(100);
        shard.UpdateProjectionLineOffset(100);
        shard.ProjectionLineOffset.Should().Be(100);
    }

    [Fact]
    public void Given_A_Projection_Cursor_When_Moving_Backwards_Then_It_Fails()
    {
        var shard = new MusicBrainzDumpImportShardState(
            MusicBrainzDumpImportPhase.Artists,
            0,
            lineOffset: 200,
            projectionLineOffset: 50);

        var act = () => shard.UpdateProjectionLineOffset(49);

        act.Should().Throw<ArgumentOutOfRangeException>();
        shard.ProjectionLineOffset.Should().Be(50);
    }

    [Fact]
    public void Given_Projection_Byte_Offset_When_Resetting_Projection_Then_Bytes_Clear()
    {
        var shard = new MusicBrainzDumpImportShardState(
            MusicBrainzDumpImportPhase.Recordings,
            0,
            lineOffset: 1_000,
            projectionLineOffset: 400,
            lineByteOffset: 50_000,
            projectionByteOffset: 20_000);

        shard.ResetProjectionForRerun();

        shard.LineOffset.Should().Be(1_000);
        shard.LineByteOffset.Should().Be(50_000);
        shard.ProjectionLineOffset.Should().Be(0);
        shard.ProjectionByteOffset.Should().Be(0);
    }
}
