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
    public async Task Given_A_New_Execution_Command_When_Handled_Then_The_Outcome_Is_Completed()
    {
        var result = await HandleNewCommand();

        result.Outcome.Should().Be(LookupExecutionOutcome.Completed);
    }

    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_A_Response_Is_Returned()
    {
        var result = await HandleNewCommand();

        result.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_The_Response_SourceProvider_Is_MusicBrainz()
    {
        var result = await HandleNewCommand();

        result.Response!.SourceProvider.Should().Be(ProviderName.MusicBrainz);
    }

    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_The_Response_Priority_Is_Preserved()
    {
        var result = await HandleNewCommand();

        result.Response!.Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_The_Response_MusicCatalogId_Is_Preserved()
    {
        var result = await HandleNewCommand();

        result.Response!.MusicCatalogId.Should().Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_A_Started_Receipt_Is_Recorded()
    {
        var state = new LookupExecutionReceiptStoreFake.State();
        var handler = new ExecuteMusicBrainzLookupHandler(new LookupExecutionReceiptStoreFake(state));

        await handler.Handle(Command());

        state.StartedReceipts.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_New_Execution_Command_When_Handled_Then_A_Completed_Receipt_Is_Recorded()
    {
        var state = new LookupExecutionReceiptStoreFake.State();
        var handler = new ExecuteMusicBrainzLookupHandler(new LookupExecutionReceiptStoreFake(state));

        await handler.Handle(Command());

        state.CompletedReceipts.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Duplicate_Execution_Command_When_Handled_Then_The_Outcome_Is_Duplicate()
    {
        var duplicate = await HandleDuplicateCommand();

        duplicate.Outcome.Should().Be(LookupExecutionOutcome.Duplicate);
    }

    [Fact]
    public async Task Given_A_Duplicate_Execution_Command_When_Handled_Then_Only_One_Started_Receipt_Is_Recorded()
    {
        var state = new LookupExecutionReceiptStoreFake.State();
        var handler = new ExecuteMusicBrainzLookupHandler(new LookupExecutionReceiptStoreFake(state));
        var command = Command();

        await handler.Handle(command);
        await handler.Handle(command);

        state.StartedReceipts.Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Duplicate_Execution_Command_When_Handled_Then_Only_One_Completed_Receipt_Is_Recorded()
    {
        var state = new LookupExecutionReceiptStoreFake.State();
        var handler = new ExecuteMusicBrainzLookupHandler(new LookupExecutionReceiptStoreFake(state));
        var command = Command();

        await handler.Handle(command);
        await handler.Handle(command);

        state.CompletedReceipts.Should().ContainSingle();
    }

    private static ResolveCanonicalMetadataCommand Command() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 5, 10, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-1"));

    private static async Task<LookupExecutionResult> HandleNewCommand()
    {
        var state = new LookupExecutionReceiptStoreFake.State();
        var handler = new ExecuteMusicBrainzLookupHandler(new LookupExecutionReceiptStoreFake(state));
        return await handler.Handle(Command());
    }

    private static async Task<LookupExecutionResult> HandleDuplicateCommand()
    {
        var state = new LookupExecutionReceiptStoreFake.State();
        var handler = new ExecuteMusicBrainzLookupHandler(new LookupExecutionReceiptStoreFake(state));
        var command = Command();

        await handler.Handle(command);
        return await handler.Handle(command);
    }
}
