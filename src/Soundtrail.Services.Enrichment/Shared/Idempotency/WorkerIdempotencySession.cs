using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Shared.Idempotency;

public abstract class WorkerIdempotencySession : IAsyncDisposable
{
    public abstract bool ProcessedBefore { get; }

    public static async Task<WorkerIdempotencySession> StartAsync(
        ILookupExecutionReceiptStore store,
        ExecuteLookupMusicCommand command,
        CancellationToken cancellationToken)
    {
        if (await store.TryBeginAsync(command.CommandId, cancellationToken))
        {
            return new StartedWorkerIdempotencySession(store, command);
        }

        return NotStartedWorkerIdempotencySession.Instance;
    }

    public abstract ValueTask DisposeAsync();

    private sealed class StartedWorkerIdempotencySession(
        ILookupExecutionReceiptStore lookupExecutionReceiptStore,
        ExecuteLookupMusicCommand command) : WorkerIdempotencySession
    {
        public override bool ProcessedBefore => false;

        public override async ValueTask DisposeAsync()
        {
            await lookupExecutionReceiptStore.MarkCompletedAsync(
                command.CommandId,
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
