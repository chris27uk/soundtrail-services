using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

public abstract class WorkerIdempotencySession : IAsyncDisposable
{
    public abstract bool ProcessedBefore { get; }

    public static async Task<WorkerIdempotencySession> StartAsync(
        ILookupExecutionReceiptStore store,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        if (await store.TryBeginAsync(commandId, cancellationToken))
        {
            return new StartedWorkerIdempotencySession(store, commandId);
        }

        return NotStartedWorkerIdempotencySession.Instance;
    }

    public abstract ValueTask DisposeAsync();

    private sealed class StartedWorkerIdempotencySession(
        ILookupExecutionReceiptStore lookupExecutionReceiptStore,
        CommandId commandId) : WorkerIdempotencySession
    {
        public override bool ProcessedBefore => false;

        public override async ValueTask DisposeAsync()
        {
            await lookupExecutionReceiptStore.MarkCompletedAsync(
                commandId,
                CancellationToken.None);
        }
    }

    private sealed class NotStartedWorkerIdempotencySession : WorkerIdempotencySession
    {
        public static readonly NotStartedWorkerIdempotencySession Instance = new();

        public override bool ProcessedBefore => true;

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
