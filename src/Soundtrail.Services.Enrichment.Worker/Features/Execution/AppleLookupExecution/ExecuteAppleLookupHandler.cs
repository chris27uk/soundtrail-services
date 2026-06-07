using Soundtrail.Contracts.Worker;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

namespace Soundtrail.Services.Enrichment.Worker.Features.Execution.AppleLookupExecution;

public sealed class ExecuteAppleLookupHandler(ILookupExecutionReceiptStore lookupExecutionReceiptStore)
{
    public async Task<LookupExecutionResult> Handle(
        ResolveApplePlaybackReferenceCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var idempotencySession = await WorkerIdempotencySession.StartAsync(
            lookupExecutionReceiptStore,
            command.CommandId,
            cancellationToken);

        if (idempotencySession.ProcessedBefore)
        {
            return LookupExecutionResult.Duplicate();
        }

        return LookupExecutionResult.Completed(
            new EnrichmentResponse(
                CommandId.From(command.CommandId),
                MusicCatalogId.From(command.MusicCatalogId),
                ProviderName.AppleMusic.Value,
                command.Priority,
                command.CreatedAt,
                null,
                [],
                CorrelationId.From(command.CorrelationId)));
    }
}
