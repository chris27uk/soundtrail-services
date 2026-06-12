using Soundtrail.Domain.Commands;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.GetReference;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;

public sealed class ExecutePlaybackReferencesLookupHandler(
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    IGetMusicTrackReference getMusicTrackReference)
{
    public async Task<LookupExecutionResult> Handle(
        ResolvePlaybackReferencesCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var idempotencySession = await IdempotencySession.StartAsync(lookupExecutionReceiptStore, command.CommandId, cancellationToken);

        if (idempotencySession.ProcessedBefore)
        {
            return LookupExecutionResult.Duplicate();
        }

        var references = await getMusicTrackReference.GetReferenceToMusicTrack(command.LookupKey, cancellationToken);
        return LookupExecutionResult.Completed(command.ToEnrichmentResponse(references));
    }
}
