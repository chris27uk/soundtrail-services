using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling;

public sealed class LookupSchedulerOrchestrator(
    LookupSchedulerHandler handler,
    IActiveLookupWorkStore activeLookupWorkStore,
    ILookupMusicCommandQueue lookupMusicCommandQueue)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public async Task<LookupMusicCommand?> HandleAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = await handler.Handle(request, cancellationToken);
        if (command is null)
        {
            return null;
        }

        var reserved = await activeLookupWorkStore.TryReserveAsync(
            command.MusicCatalogId,
            command.CommandId,
            request.OccurredAt.Add(ActiveReservationDuration),
            cancellationToken);

        if (!reserved)
        {
            return null;
        }

        await lookupMusicCommandQueue.EnqueueAsync(command, cancellationToken);
        return command;
    }
}
