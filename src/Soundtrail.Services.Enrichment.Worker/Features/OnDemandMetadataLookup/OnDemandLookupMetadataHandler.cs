using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Lookup;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup;

public sealed class OnDemandLookupMetadataHandler(
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    IGetCanonicalMusicMetadata getMetaData,
    IReserveSourceApiBudgetPort reserveSourceApiBudgetPort)
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

        var reservation = await reserveSourceApiBudgetPort.TryReserveAsync(
            new SourceApiBudgetReservationRequest(ProviderName.MusicBrainz, command.CreatedAt),
            cancellationToken);

        if (!reservation.Accepted)
        {
            return MusicCatalogLookupAttempted.Deferred(
                command.CommandId,
                command.MusicCatalogId,
                ProviderName.MusicBrainz,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId,
                reservation.Reason,
                reservation.RetryAt,
                reservation.RetryAfterSecondsFrom(command.CreatedAt));
        }

        try
        {
            var songMetadata = await getMetaData.GetMetadataAsync(command.SearchTerm, cancellationToken);
            return MusicCatalogLookupAttempted.Completed(command.ToMusicCatalogMetadataFetched(songMetadata));
        }
        catch
        {
            return MusicCatalogLookupAttempted.Failed(
                command.CommandId,
                command.MusicCatalogId,
                ProviderName.MusicBrainz,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId,
                "Lookup failed");
        }
    }
}
