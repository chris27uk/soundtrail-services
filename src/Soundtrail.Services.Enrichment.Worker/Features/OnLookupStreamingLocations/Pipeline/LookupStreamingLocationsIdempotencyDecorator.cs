using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Pipeline;

public sealed class LookupStreamingLocationsIdempotencyDecorator(
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    ILookupStreamingLocationsHandler inner) : ILookupStreamingLocationsHandler
{
    public async Task<MusicCatalogLookupAttempted> Handle(
        LookupStreamingLocationsCommand command,
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
                command.TargetProvider,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId);
        }

        return await inner.Handle(command, cancellationToken);
    }
}
