using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling;

public sealed class LookupPlanningSweep(
    IRankedMusicCandidateStore rankedMusicCandidateStore,
    IActiveLookupWorkStore activeLookupWorkStore,
    ILookupMusicCommandQueue lookupMusicCommandQueue,
    LookupPlanner lookupPlanner)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public async Task<int> RunOnceAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken = default)
    {
        var candidates = await rankedMusicCandidateStore.GetPlanningCandidatesAsync(
            now,
            take,
            cancellationToken);

        var emitted = 0;
        foreach (var candidate in candidates)
        {
            var plan = lookupPlanner.Plan(candidate, now);
            if (!plan.ShouldSchedule)
            {
                continue;
            }

            var command = new LookupMusicCommand(
                CommandId: Guid.NewGuid().ToString("N"),
                MusicCatalogId: candidate.MusicCatalogId,
                Priority: plan.Priority!.Value,
                CreatedAt: now,
                CorrelationId: $"sweep:{candidate.MusicCatalogId.Value}:{now:yyyyMMddHHmmss}");

            var reserved = await activeLookupWorkStore.TryReserveAsync(
                candidate.MusicCatalogId,
                command.CommandId,
                now.Add(ActiveReservationDuration),
                cancellationToken);

            if (!reserved)
            {
                continue;
            }

            await lookupMusicCommandQueue.EnqueueAsync(command, cancellationToken);
            emitted++;
        }

        return emitted;
    }
}
