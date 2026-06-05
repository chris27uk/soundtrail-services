using Soundtrail.Services.Enrichment.Features.Execution.Idempotency;
using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Features.Execution;

public sealed class ExecuteYouTubeMusicLookupHandler(ILookupExecutionReceiptStore lookupExecutionReceiptStore)
{
    public async Task<LookupExecutionResult> Handle(
        ExecuteLookupMusicCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var idempotencySession = await WorkerIdempotencySession.StartAsync(
            lookupExecutionReceiptStore,
            command,
            cancellationToken);

        if (idempotencySession.ProcessedBefore)
        {
            return LookupExecutionResult.Duplicate();
        }

        return LookupExecutionResult.Completed();
    }
}
