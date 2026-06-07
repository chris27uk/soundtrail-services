using Soundtrail.Contracts;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

public interface ILookupExecutionReceiptStore
{
    Task<bool> TryBeginAsync(
        CommandId commandId,
        CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        CommandId commandId,
        CancellationToken cancellationToken);
}
