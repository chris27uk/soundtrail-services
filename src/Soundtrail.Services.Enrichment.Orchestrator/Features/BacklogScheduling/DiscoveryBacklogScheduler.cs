using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling;

public sealed class DiscoveryBacklogScheduler(
    IPotentialCatalogLookupWorkStore rankedMusicCandidateStore,
    IActiveLookupWorkStore activeLookupWorkStore,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    IReserveSourceApiBudgetPort reserveSourceApiBudgetPort,
    ILocalMusicTrackSearch localMusicTrackSearch,
    DiscoveryBacklogLookupPlanner lookupPlanner,
    TrackedDiscoveryStartMarker trackedDiscoveryStartMarker)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public async Task<IReadOnlyList<IMusicCatalogLookupCommand>> RunOnceAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken = default)
    {
        var candidates = await rankedMusicCandidateStore.GetPlanningCandidatesAsync(now, take, cancellationToken);
        var commands = new List<IMusicCatalogLookupCommand>();

        foreach (var candidate in candidates)
        {
            var plan = discoveryPriorityPolicy.Investigate(candidate, now);
            if (!plan.ShouldSchedule)
            {
                continue;
            }

            var localTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(candidate.MusicCatalogId, cancellationToken);
            var plannedLookup = lookupPlanner.Plan(candidate.MusicCatalogId, plan.Priority!.Value, now, localTrack);

            if (plannedLookup is null)
            {
                continue;
            }

            var budgetReservation = await reserveSourceApiBudgetPort.TryReserveAsync(
                new SourceApiBudgetReservationRequest(plannedLookup.Source, now),
                cancellationToken);
            if (!budgetReservation.Accepted)
            {
                continue;
            }

            var acquired = await activeLookupWorkStore.TryAcquireAsync(
                plannedLookup.Command.CommandId, now.Add(ActiveReservationDuration), cancellationToken);
            if (!acquired)
            {
                continue;
            }

            await trackedDiscoveryStartMarker.MarkAsync(
                plannedLookup.Command.MusicCatalogId,
                plannedLookup.Command.Priority,
                now,
                cancellationToken);

            commands.Add(plannedLookup.Command);
        }

        return commands;
    }
}
