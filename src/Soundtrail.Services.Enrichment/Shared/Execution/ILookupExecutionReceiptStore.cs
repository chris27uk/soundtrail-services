using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Execution;

public interface ILookupExecutionReceiptStore
{
    Task<bool> TryBeginAsync(
        CommandId commandId,
        CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        CommandId commandId,
        CancellationToken cancellationToken);
}
