using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Shared.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.Execution.YouTubeMusicLookupExecution;

public sealed class ExecuteYouTubeMusicLookupHandler(ILookupExecutionReceiptStore lookupExecutionReceiptStore)
{
    public async Task<LookupExecutionResult> Handle(
        ResolveYouTubeMusicPlaybackReferenceCommand command,
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
                ProviderName.YouTubeMusic,
                command.Priority,
                command.CreatedAt,
                null,
                [],
                command.CorrelationId));
    }
}
