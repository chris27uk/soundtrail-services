using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Shared.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.Execution.AppleLookupExecution;

public sealed class ExecuteAppleLookupHandler(ILookupExecutionReceiptStore lookupExecutionReceiptStore)
{
    public async Task<LookupExecutionResult> Handle(
        VerifyApplePlaybackReferenceCommand command,
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

        return LookupExecutionResult.Completed(
            new EnrichmentResponse(
                command.CommandId,
                command.MusicCatalogId,
                ProviderName.Apple,
                command.Priority,
                command.CreatedAt,
                null,
                [],
                command.CorrelationId));
    }
}
