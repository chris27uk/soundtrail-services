using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Pipeline;

public sealed class LookupMusicMetadataIdempotencyDecorator(
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    ILookupMusicMetadataHandler inner) : ILookupMusicMetadataHandler
{
    public async Task<MusicCatalogLookupAttempted> Handle(
        LookupMusicMetadataCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var idempotencySession = await IdempotencySession.StartAsync(
            lookupExecutionReceiptStore,
            command.CommandId,
            cancellationToken);

        if (idempotencySession.ProcessedBefore)
        {
            return MusicCatalogLookupAttempted.Duplicate(
                command.CommandId,
                command.MusicCatalogId,
                ProviderName.MusicBrainz,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId);
        }

        return await inner.Handle(command, cancellationToken);
    }
}
