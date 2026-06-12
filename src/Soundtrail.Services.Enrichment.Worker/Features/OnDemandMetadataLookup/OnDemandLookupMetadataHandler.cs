using Soundtrail.Domain.Commands;
using Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup;

public sealed class OnDemandLookupMetadataHandler(
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    IGetCanonicalMusicMetadata getMetaData)
{
    public async Task<LookupExecutionResult> Handle(
        LookupMusicMetadataCommand command,
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

        var songMetadata = await getMetaData.GetMetadataAsync(command.SearchTerm, cancellationToken);
        return LookupExecutionResult.Completed(command.ToEnrichmentResponse(songMetadata));
    }
}
