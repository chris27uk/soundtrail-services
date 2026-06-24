using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Pipeline;

public sealed class LookupStreamingLocationsBudgetReservationDecorator(
    IReserveSourceApiBudgetPort reserveSourceApiBudgetPort,
    ILookupStreamingLocationsHandler inner) : ILookupStreamingLocationsHandler
{
    public async Task<MusicCatalogLookupAttempted> Handle(
        LookupStreamingLocationsCommand command,
        CancellationToken cancellationToken = default)
    {
        var reservation = await reserveSourceApiBudgetPort.TryReserveAsync(
            new SourceApiBudgetReservationRequest(command.TargetProvider, command.CreatedAt),
            cancellationToken);

        if (!reservation.Accepted)
        {
            return MusicCatalogLookupAttempted.Deferred(
                command.CommandId,
                command.MusicCatalogId,
                command.TargetProvider,
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
