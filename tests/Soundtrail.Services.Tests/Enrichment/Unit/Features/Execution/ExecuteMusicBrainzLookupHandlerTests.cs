using FluentAssertions;
using Soundtrail.Services.Enrichment.Features.Execution;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Features.Execution;

public sealed class ExecuteMusicBrainzLookupHandlerTests
{
    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_It_Is_Completed()
    {
        var state = new LookupExecutionReceiptStoreFake.State();
        var handler = new ExecuteMusicBrainzLookupHandler(new LookupExecutionReceiptStoreFake(state));

        var result = await handler.Handle(Command());

        result.Outcome.Should().Be(LookupExecutionOutcome.Completed);
        state.StartedReceipts.Should().ContainSingle();
        state.CompletedReceipts.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Duplicate_Execution_Command_When_Handled_Then_It_Is_Skipped()
    {
        var state = new LookupExecutionReceiptStoreFake.State();
        var handler = new ExecuteMusicBrainzLookupHandler(new LookupExecutionReceiptStoreFake(state));
        var command = Command();

        await handler.Handle(command);
        var duplicate = await handler.Handle(command);

        duplicate.Outcome.Should().Be(LookupExecutionOutcome.Duplicate);
        state.StartedReceipts.Should().ContainSingle();
        state.CompletedReceipts.Should().ContainSingle();
    }

    private static ExecuteLookupMusicCommand Command() =>
        new(
            CommandId.For("MusicBrainz:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 10, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-1"));
}
