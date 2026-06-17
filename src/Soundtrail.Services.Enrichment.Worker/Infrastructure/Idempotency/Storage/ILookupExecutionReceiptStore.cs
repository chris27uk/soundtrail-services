using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

public interface ILookupExecutionReceiptStore
{
    Task<bool> TryBeginAsync(
        CommandId commandId,
        CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        CommandId commandId,
        CancellationToken cancellationToken);
}
