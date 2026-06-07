using Soundtrail.Contracts.Worker;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Worker.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

namespace Soundtrail.Services.Enrichment.Worker.Features.Execution.MusicBrainzLookupExecution;

public sealed class ExecuteMusicBrainzLookupHandler(ILookupExecutionReceiptStore lookupExecutionReceiptStore)
{
    public async Task<LookupExecutionResult> Handle(
        ResolveCanonicalMetadataCommand command,
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
            new EnrichmentResponseDto(
                CommandId.From(command.CommandId),
                MusicCatalogId.From(command.MusicCatalogId),
                ProviderName.MusicBrainz.Value,
                command.Priority,
                command.CreatedAt,
                null,
                [],
                CorrelationId.From(command.CorrelationId)));
    }
}
