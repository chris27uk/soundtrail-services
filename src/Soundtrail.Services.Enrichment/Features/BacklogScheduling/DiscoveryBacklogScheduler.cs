using Soundtrail.Services.Enrichment.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Shared.Persistence;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Queuing;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Features.BacklogScheduling;

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
            var plan = discoveryPriorityPolicy.Plan(candidate, now);
            if (!plan.ShouldSchedule)
            {
                continue;
            }

            var command = BuildCommand(candidate, plan, now);
            var acquired = await activeLookupWorkStore.TryAcquireAsync(
                command.CommandId,
                now.Add(ActiveReservationDuration),
                cancellationToken);
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
        LookupPlan plan,
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
