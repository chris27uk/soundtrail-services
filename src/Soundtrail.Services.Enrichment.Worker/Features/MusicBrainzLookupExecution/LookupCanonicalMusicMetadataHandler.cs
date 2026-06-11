using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;

namespace Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution;

public sealed class LookupCanonicalMusicMetadataHandler(
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    IGetCanonicalMusicMetadata getMetaData)
{
    public async Task<LookupExecutionResult> Handle(
        LookupCanonicalMusicMetadataCommand command,
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

        var songMetadata = await ResolveMetadataAsync(command.SearchTerm, cancellationToken);
        return LookupExecutionResult.Completed(
            new EnrichmentResponse(
                command.CommandId,
                command.MusicCatalogId,
                ProviderName.MusicBrainz,
                command.Priority,
                command.CreatedAt,
                songMetadata,
                [],
                command.CorrelationId));
    }

    private async Task<SongMetadata?> ResolveMetadataAsync(
        MusicSearchTerm searchTerm,
        CancellationToken cancellationToken)
        => await getMetaData.GetMetadataAsync(searchTerm, cancellationToken);
}
