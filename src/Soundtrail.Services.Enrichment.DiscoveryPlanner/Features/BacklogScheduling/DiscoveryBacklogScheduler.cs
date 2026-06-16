using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling;

public sealed class DiscoveryBacklogScheduler(
    IPotentialCatalogLookupWorkStore rankedMusicCandidateStore,
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
        PotentialCatalogLookupWork candidate,
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
                playbackLookupKey,
                ToHierarchy(localTrack));
        }

        if (playbackLookupKey is null)
        {
            return null;
        }

        return new LookupMusicMetadataCommand(
            CommandId.For($"LookupCanonicalMusicMetadata:{candidate.MusicCatalogId.Value}"),
            candidate.MusicCatalogId,
            plan.Priority!.Value,
            now,
            CorrelationId.New(),
            playbackLookupKey,
            ToHierarchy(localTrack));
    }

    private static CatalogTrackHierarchy? ToHierarchy(LocalMusicTrackSearchResult? localTrack) =>
        localTrack?.ArtistId is null && localTrack?.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(localTrack?.ArtistId, localTrack?.AlbumId);
}
