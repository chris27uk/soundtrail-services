using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling;

public sealed class DiscoveryBacklogScheduler(
    IRankedMusicCandidateStore rankedMusicCandidateStore,
    IActiveLookupWorkStore activeLookupWorkStore,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    ILocalMusicTrackSearch localMusicTrackSearch)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public async Task<IReadOnlyList<LookupPhaseCommand>> RunOnceAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken = default)
    {
        var candidates = await rankedMusicCandidateStore.GetPlanningCandidatesAsync(now, take, cancellationToken);
        var commands = new List<LookupPhaseCommand>();

        foreach (var candidate in candidates)
        {
            var plan = discoveryPriorityPolicy.Investigate(candidate, now);
            if (!plan.ShouldSchedule)
            {
                continue;
            }

            var localTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(candidate.MusicCatalogId, cancellationToken);
            var command = BuildCommand(candidate, plan, now, localTrack);
            if (command is null)
            {
                continue;
            }

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

    private static LookupPhaseCommand? BuildCommand(
        RankedMusicCandidate candidate,
        PriorityPlan plan,
        DateTimeOffset now,
        LocalMusicTrackSearchResult? localTrack)
    {
        if (localTrack?.IsPlayable == true)
        {
            return null;
        }

        var playbackLookupKey = localTrack?.GetSearchTerm();
        if ((localTrack != null ? !string.IsNullOrWhiteSpace(localTrack.Isrc) : null) == true && playbackLookupKey is not null)
        {
            return new ResolvePlaybackReferencesCommand(
                CommandId.For($"ResolvePlaybackReferences:{candidate.MusicCatalogId.Value}"),
                candidate.MusicCatalogId,
                plan.Priority!.Value,
                now,
                CorrelationId.New(),
                playbackLookupKey);
        }

        if (playbackLookupKey is null)
        {
            return null;
        }

        return new LookupCanonicalMusicMetadataCommand(
            CommandId.For($"LookupCanonicalMusicMetadata:{candidate.MusicCatalogId.Value}"),
            candidate.MusicCatalogId,
            plan.Priority!.Value,
            now,
            CorrelationId.New(),
            playbackLookupKey);
    }
}
