using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.Execution;
using Soundtrail.Services.Enrichment.Worker.Features.Execution.MusicBrainzLookupExecution;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.MusicBrainzLookupExecution;

public sealed class ExecuteMusicBrainzLookupHandlerTests
{
    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_It_Is_Completed()
    {
        var state = new LookupExecutionReceiptStoreFake.State();
        var handler = new ExecuteMusicBrainzLookupHandler(new LookupExecutionReceiptStoreFake(state));

        var result = await handler.Handle(Command());

        result.Outcome.Should().Be(LookupExecutionOutcome.Completed);
        result.Response.Should().NotBeNull();
        result.Response!.SourceProvider.Should().Be(ProviderName.MusicBrainz);
        result.Response.Priority.Should().Be(LookupPriorityBand.High);
        result.Response.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
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

    private static ResolveCanonicalMetadataCommand Command() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 10, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-1"));
}
