using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

public abstract class IdempotencySession : IAsyncDisposable
{
    public abstract bool ProcessedBefore { get; }

    public static async Task<IdempotencySession> StartAsync(
        ILookupExecutionReceiptStore store,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        if (await store.TryBeginAsync(commandId, cancellationToken))
        {
            return new StartedIdempotencySession(store, commandId);
        }

        return NotStartedIdempotencySession.Instance;
    }

    public abstract ValueTask DisposeAsync();

    private sealed class StartedIdempotencySession(
        ILookupExecutionReceiptStore lookupExecutionReceiptStore,
        CommandId commandId) : IdempotencySession
    {
        public override bool ProcessedBefore => false;

        public override async ValueTask DisposeAsync()
        {
            await lookupExecutionReceiptStore.MarkCompletedAsync(
                commandId,
                CancellationToken.None);
        }
    }

    private sealed class NotStartedIdempotencySession : IdempotencySession
    {
        public static readonly NotStartedIdempotencySession Instance = new();

        public override bool ProcessedBefore => true;

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
