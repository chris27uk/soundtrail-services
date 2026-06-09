using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;

public sealed class ExecutePlaybackReferencesLookupHandler(
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    IPlaybackReferenceSource playbackReferenceSource)
{
    public async Task<LookupExecutionResult> Handle(
        ResolvePlaybackReferencesCommand command,
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

        var references = await playbackReferenceSource.GetPlaybackReferencesAsync(
            command.LookupKey,
            cancellationToken);

        return LookupExecutionResult.Completed(
            new EnrichmentResponse(
                command.CommandId,
                command.MusicCatalogId,
                command.TargetProvider,
                command.Priority,
                command.CreatedAt,
                null,
                references,
                command.CorrelationId));
    }
}
