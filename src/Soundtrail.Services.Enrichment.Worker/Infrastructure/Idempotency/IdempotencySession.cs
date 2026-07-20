using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

public abstract class IdempotencySession : IAsyncDisposable
{
    public abstract bool ProcessedBefore { get; }

    public abstract Task CompleteAsync(CancellationToken cancellationToken);

    public abstract Task ReleaseAsync(CancellationToken cancellationToken);

    public static async Task<IdempotencySession> StartAsync(
        ILookupExecutionReceiptStore store,
        MessageId messageId,
        CancellationToken cancellationToken)
    {
        if (await store.TryBeginAsync(messageId, cancellationToken))
        {
            return new StartedIdempotencySession(store, messageId);
        }

        return NotStartedIdempotencySession.Instance;
    }

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private sealed class StartedIdempotencySession(
        ILookupExecutionReceiptStore lookupExecutionReceiptStore,
        MessageId messageId) : IdempotencySession
    {
        public override bool ProcessedBefore => false;

        public override Task CompleteAsync(CancellationToken cancellationToken) =>
            lookupExecutionReceiptStore.MarkCompletedAsync(messageId, cancellationToken);

        public override Task ReleaseAsync(CancellationToken cancellationToken) =>
            lookupExecutionReceiptStore.ReleaseAsync(messageId, cancellationToken);
    }

    private sealed class NotStartedIdempotencySession : IdempotencySession
    {
        public static readonly NotStartedIdempotencySession Instance = new();

        public override bool ProcessedBefore => true;

        public override Task CompleteAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override Task ReleaseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
