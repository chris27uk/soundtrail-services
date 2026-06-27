using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogDiscoveryWorkDispatchRequested;

public sealed class CatalogDiscoveryWorkDispatchRequestedHandler(
    ICatalogDiscoveryWorkPlanningReadPort planningReadPort,
    ILocalMusicTrackSearch localMusicTrackSearch,
    DiscoveryBacklogLookupPlanner lookupPlanner,
    IActiveLookupWorkStore activeLookupWorkStore,
    ICommandBus commandBus)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public async Task<bool> Handle(
        DispatchCatalogDiscoveryWorkCommand command,
        CancellationToken cancellationToken = default)
    {
        var summary = await planningReadPort.LoadAsync(command.MusicCatalogId, cancellationToken);
        if (summary is null
            || summary.Status != CatalogDiscoveryWorkStatus.Pending
            || summary.Priority is null
            || summary.NextEligibleAt is not null)
        {
            return false;
        }

        var localTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(command.MusicCatalogId, cancellationToken);
        var plannedLookup = lookupPlanner.Plan(command.MusicCatalogId, summary.Priority.Value, command.RequestedAt, localTrack, command.CorrelationId);
        if (plannedLookup is null)
        {
            return false;
        }

        var acquired = await activeLookupWorkStore.TryAcquireAsync(
            plannedLookup.Command.CommandId,
            command.RequestedAt.Add(ActiveReservationDuration),
            cancellationToken);

        if (!acquired)
        {
            return false;
        }

        await commandBus.SendAsync(plannedLookup.Command, cancellationToken);
        return true;
    }
}
