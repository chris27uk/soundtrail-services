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
}
