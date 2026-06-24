using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Pipeline;

public sealed class LookupMusicMetadataBudgetReservationDecorator(
    IReserveSourceApiBudgetPort reserveSourceApiBudgetPort,
    ILookupMusicMetadataHandler inner) : ILookupMusicMetadataHandler
{ 
    public async Task<MusicCatalogLookupAttempted> Handle(
        LookupMusicMetadataCommand command,
        CancellationToken cancellationToken = default)
    {
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

        return await inner.Handle(command, cancellationToken);
    }
}
