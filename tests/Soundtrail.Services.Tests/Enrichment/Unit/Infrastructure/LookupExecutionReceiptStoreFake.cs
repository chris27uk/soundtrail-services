using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

internal sealed class LookupExecutionReceiptStoreFake(
    LookupExecutionReceiptStoreFake.State state) : ILookupExecutionReceiptStore
{
    public Task<bool> TryBeginAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(state.StartedReceipts.Add(commandId.Value));
    }

    public Task MarkCompletedAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        state.CompletedReceipts.Add(commandId.Value);
        return Task.CompletedTask;
    }

    internal sealed class State
    {
        public HashSet<string> StartedReceipts { get; } = [];

        public HashSet<string> CompletedReceipts { get; } = [];
    }
}
