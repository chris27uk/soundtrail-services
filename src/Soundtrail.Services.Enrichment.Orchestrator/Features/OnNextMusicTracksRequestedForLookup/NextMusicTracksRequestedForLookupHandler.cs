using Soundtrail.Domain;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup;

public sealed class NextMusicTracksRequestedForLookupHandler(
    IPotentialCatalogLookupWorkStore rankedMusicCandidateStore,
    IActiveLookupWorkStore activeLookupWorkStore,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    ILocalMusicTrackSearch localMusicTrackSearch,
    DiscoveryBacklogLookupPlanner lookupPlanner,
    ICommandBus commandBus)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public async Task RunOnceAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default)
    {
        var candidates = await rankedMusicCandidateStore.GetPlanningCandidatesAsync(now, take, cancellationToken);

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

            var acquired = await activeLookupWorkStore.TryAcquireAsync(plannedLookup.Command.CommandId, now.Add(ActiveReservationDuration), cancellationToken);
            if (!acquired)
            {
                continue;
            }

            await commandBus.SendAsync(plannedLookup.Command, cancellationToken);
        }
    }
}
