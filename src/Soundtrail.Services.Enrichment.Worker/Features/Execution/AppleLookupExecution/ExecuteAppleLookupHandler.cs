using Soundtrail.Contracts.Worker;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Worker.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

namespace Soundtrail.Services.Enrichment.Worker.Features.Execution.AppleLookupExecution;

public sealed class ExecuteAppleLookupHandler(ILookupExecutionReceiptStore lookupExecutionReceiptStore)
{
    public async Task<LookupExecutionResult> Handle(
        ResolveApplePlaybackReferenceCommandDto commandDto,
        CancellationToken cancellationToken = default)
    {
        await using var idempotencySession = await WorkerIdempotencySession.StartAsync(
            lookupExecutionReceiptStore,
            commandDto.CommandId,
            cancellationToken);

        if (idempotencySession.ProcessedBefore)
        {
            return LookupExecutionResult.Duplicate();
        }

        return LookupExecutionResult.Completed(
            new EnrichmentResponseDto(
                CommandId.From(commandDto.CommandId),
                MusicCatalogId.From(commandDto.MusicCatalogId),
                ProviderName.AppleMusic.Value,
                commandDto.Priority,
                commandDto.CreatedAt,
                null,
                [],
                CorrelationId.From(commandDto.CorrelationId)));
    }
}
