using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling;

public sealed class DiscoveryBacklogScheduler(
    IRankedMusicCandidateStore rankedMusicCandidateStore,
    IActiveLookupWorkStore activeLookupWorkStore,
    DiscoveryPriorityPolicy discoveryPriorityPolicy)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public async Task<IReadOnlyList<LookupMusicCommand>> RunOnceAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken = default)
    {
        var candidates = await rankedMusicCandidateStore.GetPlanningCandidatesAsync(now, take, cancellationToken);
        var commands = new List<LookupMusicCommand>();

        foreach (var candidate in candidates)
        {
            var plan = discoveryPriorityPolicy.Investigate(candidate, now);
            if (!plan.ShouldSchedule)
            {
                continue;
            }

            var command = BuildCommand(candidate, plan, now);
            var acquired = await activeLookupWorkStore.TryAcquireAsync(
                command.CommandId, now.Add(ActiveReservationDuration), cancellationToken);
            if (!acquired)
            {
                continue;
            }

            commands.Add(command);
        }

        return commands;
    }

    private static LookupMusicCommand BuildCommand(
        RankedMusicCandidate candidate,
        PriorityPlan plan,
        DateTimeOffset now)
    {
        return new LookupMusicCommand(
            CommandId: CommandId.For(candidate.MusicCatalogId.Value),
            MusicCatalogId: candidate.MusicCatalogId,
            Priority: plan.Priority!.Value,
            CreatedAt: now,
            CorrelationId: CorrelationId.New());
    }
}
