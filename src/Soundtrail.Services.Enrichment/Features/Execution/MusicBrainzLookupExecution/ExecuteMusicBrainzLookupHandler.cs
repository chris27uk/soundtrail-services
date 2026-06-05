using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Shared.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.Execution.MusicBrainzLookupExecution;

public sealed class ExecuteMusicBrainzLookupHandler(ILookupExecutionReceiptStore lookupExecutionReceiptStore)
{
    public async Task<LookupExecutionResult> Handle(
        ResolveCanonicalMetadataCommand command,
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
                ProviderName.MusicBrainz,
                command.Priority,
                command.CreatedAt,
                null,
                [],
                command.CorrelationId));
    }
}
