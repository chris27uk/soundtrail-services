using Soundtrail.Contracts.Worker;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

namespace Soundtrail.Services.Enrichment.Worker.Features.Execution.YouTubeMusicLookupExecution;

public sealed class ExecuteYouTubeMusicLookupHandler(ILookupExecutionReceiptStore lookupExecutionReceiptStore)
{
    public async Task<LookupExecutionResult> Handle(
        ResolveYouTubeMusicPlaybackReferenceCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var idempotencySession = await WorkerIdempotencySession.StartAsync(
            lookupExecutionReceiptStore,
            CommandId.From(command.CommandId),
            cancellationToken);

        if (idempotencySession.ProcessedBefore)
        {
            return LookupExecutionResult.Duplicate();
        }

        return LookupExecutionResult.Completed(
            new EnrichmentResponse(
                CommandId.From(command.CommandId),
                MusicCatalogId.From(command.MusicCatalogId),
                ProviderName.YoutubeMusic.Value,
                command.Priority,
                command.CreatedAt,
                null,
                [],
                CorrelationId.From(command.CorrelationId)));
    }
}
