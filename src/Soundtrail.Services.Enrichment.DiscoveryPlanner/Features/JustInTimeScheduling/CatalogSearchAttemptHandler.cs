using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Scheduling;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;

public sealed class CatalogSearchAttemptHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    IPotentialCatalogLookupWorkStore potentialCatalogLookupWorkStore,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    IReserveSourceApiBudgetPort reserveSourceApiBudgetPort,
    MusicCatalogMatchResolver musicCatalogMatchResolver,
    IActiveLookupWorkStore activeLookupWorkStore,
    ILocalMusicTrackSearch localMusicTrackSearch)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public Task<LookupSchedulingResult> Handle(
        CatalogSearchAttempt request,
        CancellationToken cancellationToken = default) =>
        ScheduleAsync(request, cancellationToken);

    public async Task<LookupSchedulingResult> ScheduleAsync(
        CatalogSearchAttempt request,
        CancellationToken cancellationToken = default)
    {
        var matches = await musicCatalogCandidateSearch.SearchAsync(request.Query, cancellationToken);
        var resolution = musicCatalogMatchResolver.Resolve(matches);
        if (!resolution.IsResolved)
        {
            throw new ResolutionFailedException(resolution.Outcome);
        }

        var musicCatalogId = resolution.MusicCatalogId ?? throw new ResolutionFailedException(resolution.Outcome);
        var existing = await potentialCatalogLookupWorkStore.FindByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        var rankedMusicCandidate = existing is null
            ? PotentialCatalogLookupWork.Create(request, musicCatalogId)
            : existing.AcceptNewRequest(request);
        await potentialCatalogLookupWorkStore.UpsertAsync(rankedMusicCandidate, cancellationToken);
        var plan = discoveryPriorityPolicy.Investigate(rankedMusicCandidate, request.OccurredAt);
        if (!plan.ShouldSchedule)
        {
            await UpsertTrackingsAsync(
                CatalogSearchCriteriaSet.ForResolvedTrack(musicCatalogId, artistId: null, albumId: null, request.Criteria),
                musicCatalogId,
                request.OccurredAt,
                cancellationToken);

            return LookupSchedulingResult.DoNotSchedule(
                plan.EstimatedRetryAfterSeconds,
                plan.EarliestExpectedCompletionAt,
                plan.Reason);
        }

        var localTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        await UpsertTrackingsAsync(
            CatalogSearchCriteriaSet.ForResolvedTrack(musicCatalogId, localTrack?.ArtistId, localTrack?.AlbumId, request.Criteria),
            musicCatalogId,
            request.OccurredAt,
            cancellationToken);

        var plannedLookup = BuildCommand(request, musicCatalogId, plan.Priority!.Value, localTrack);
        if (plannedLookup is null)
        {
            var deferred = PriorityPlan.Defer(request.OccurredAt);
            return LookupSchedulingResult.DoNotSchedule(
                deferred.EstimatedRetryAfterSeconds,
                deferred.EarliestExpectedCompletionAt,
                deferred.Reason);
        }

        var budgetReservation = await reserveSourceApiBudgetPort.TryReserveAsync(
            new SourceApiBudgetReservationRequest(plannedLookup.Source, request.OccurredAt),
            cancellationToken);
        if (!budgetReservation.Accepted)
        {
            return LookupSchedulingResult.DoNotSchedule(
                budgetReservation.RetryAfterSecondsFrom(request.OccurredAt),
                budgetReservation.RetryAt,
                budgetReservation.Reason);
        }

        var acquired = await activeLookupWorkStore.TryAcquireAsync(
            plannedLookup.Command.CommandId,
            request.OccurredAt.Add(ActiveReservationDuration),
            cancellationToken);

        if (acquired)
        {
            return LookupSchedulingResult.Schedule(
                plan.EstimatedRetryAfterSeconds,
                plan.EarliestExpectedCompletionAt,
                plan.Reason,
                plannedLookup.Command);
        }

        var deferredByReservation = PriorityPlan.Defer(request.OccurredAt);
        return LookupSchedulingResult.DoNotSchedule(
            deferredByReservation.EstimatedRetryAfterSeconds,
            deferredByReservation.EarliestExpectedCompletionAt,
            deferredByReservation.Reason);
    }

    private async Task UpsertTrackingsAsync(
        IReadOnlyList<CatalogSearchCriteria> criteria,
        MusicCatalogId musicCatalogId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        foreach (var item in criteria)
        {
            await catalogSearchTrackingStore.UpsertAsync(
                new CatalogSearchTracking(item, musicCatalogId, updatedAt),
                cancellationToken);
        }
    }

    private static PlannedLookupWork? BuildCommand(
        CatalogSearchAttempt request,
        MusicCatalogId musicCatalogId,
        LookupPriorityBand priority,
        LocalMusicTrackSearchResult? localTrack)
    {
        if (localTrack?.IsPlayable == true)
        {
            return null;
        }

        var searchTerm = localTrack?.GetSearchTerm();
        if ((localTrack != null ? !string.IsNullOrWhiteSpace(localTrack.Isrc) : null) == true && searchTerm is not null)
        {
            return new PlannedLookupWork(
                new ResolvePlaybackReferencesCommand(
                    CommandId.For($"ResolvePlaybackReferences:{musicCatalogId.Value}"),
                    musicCatalogId,
                    priority,
                    request.OccurredAt,
                    request.CorrelationId,
                    searchTerm,
                    ToHierarchy(localTrack)),
                ProviderName.Odesli);
        }

        if (searchTerm is null)
        {
            return null;
        }

        return new PlannedLookupWork(
            new LookupMusicMetadataCommand(
                CommandId.For($"LookupCanonicalMusicMetadata:{musicCatalogId.Value}"),
                musicCatalogId,
                priority,
                request.OccurredAt,
                request.CorrelationId,
                searchTerm,
                ToHierarchy(localTrack)),
            ProviderName.MusicBrainz);
    }

    private static CatalogTrackHierarchy? ToHierarchy(LocalMusicTrackSearchResult? localTrack) =>
        localTrack?.ArtistId is null && localTrack?.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(localTrack?.ArtistId, localTrack?.AlbumId);
}
