using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ApplyLookupExecutionReport.Support;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Scheduling;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling;

public sealed class DiscoveryBacklogScheduler(
    IPotentialCatalogLookupWorkStore rankedMusicCandidateStore,
    IActiveLookupWorkStore activeLookupWorkStore,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    IReserveSourceApiBudgetPort reserveSourceApiBudgetPort,
    ILocalMusicTrackSearch localMusicTrackSearch,
    CatalogSearchDiscoveryByMusicCatalogIdTransitionApplier transitionApplier)
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
            var plannedLookup = BuildCommand(candidate, plan, now, localTrack);
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

            await transitionApplier.ApplyAsync(
                plannedLookup.Command.MusicCatalogId,
                discovery => discovery.Start(plannedLookup.Command.Priority, "Lookup started", now),
                cancellationToken);

            commands.Add(plannedLookup.Command);
        }

        return commands;
    }

    private static PlannedLookupWork? BuildCommand(
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
            return new PlannedLookupWork(
                new ResolvePlaybackReferencesCommand(
                    CommandId.For($"ResolvePlaybackReferences:{candidate.MusicCatalogId.Value}"),
                    candidate.MusicCatalogId,
                    plan.Priority!.Value,
                    now,
                    CorrelationId.New(),
                    playbackLookupKey,
                    ToHierarchy(localTrack)),
                ProviderName.Odesli);
        }

        if (playbackLookupKey is null)
        {
            return null;
        }

        return new PlannedLookupWork(
            new LookupMusicMetadataCommand(
                CommandId.For($"LookupCanonicalMusicMetadata:{candidate.MusicCatalogId.Value}"),
                candidate.MusicCatalogId,
                plan.Priority!.Value,
                now,
                CorrelationId.New(),
                playbackLookupKey,
                ToHierarchy(localTrack)),
            ProviderName.MusicBrainz);
    }

    private static CatalogTrackHierarchy? ToHierarchy(LocalMusicTrackSearchResult? localTrack) =>
        localTrack?.ArtistId is null && localTrack?.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(localTrack?.ArtistId, localTrack?.AlbumId);
}
