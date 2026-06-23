using Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search.Resolution;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Scheduling;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling;

public sealed class CatalogSearchAttemptHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    IPotentialCatalogLookupWorkStore potentialCatalogLookupWorkStore,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICatalogSearchDiscoveryRepository discoveryRepository,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    IReserveSourceApiBudgetPort reserveSourceApiBudgetPort,
    MusicCatalogMatchResolver musicCatalogMatchResolver,
    IActiveLookupWorkStore activeLookupWorkStore,
    ILocalMusicTrackSearch localMusicTrackSearch,
    ICommandBus commandBus)
{
    private static readonly TimeSpan ActiveReservationDuration = TimeSpan.FromMinutes(15);

    public Task<LookupSchedulingResult> Handle(
        CatalogSearchAttempt request,
        CancellationToken cancellationToken = default) =>
        HandleInternalAsync(request, cancellationToken);

    private async Task<LookupSchedulingResult> HandleInternalAsync(
        CatalogSearchAttempt request,
        CancellationToken cancellationToken)
    {
        try
        {
            var decision = await ScheduleAsync(request, cancellationToken);
            await PersistLifecycleAsync(request, decision, cancellationToken);
            if (decision.Command is not null)
            {
                await commandBus.SendAsync(decision.Command, cancellationToken);
            }

            return decision.ShouldSchedule
                ? LookupSchedulingResult.Schedule(
                    decision.Command!.Priority,
                    decision.EstimatedRetryAfterSeconds,
                    decision.EarliestExpectedCompletionAt,
                    decision.Reason)
                : LookupSchedulingResult.DoNotSchedule(
                    decision.EstimatedRetryAfterSeconds,
                    decision.EarliestExpectedCompletionAt,
                    decision.Reason);
        }
        catch (ResolutionFailedException ex)
        {
            await PersistRejectedAsync(request, ex.Outcome, cancellationToken);
            return LookupSchedulingResult.DoNotSchedule(
                estimatedRetryAfterSeconds: null,
                earliestExpectedCompletionAt: null,
                reason: ToRejectedReason(ex.Outcome));
        }
    }

    private async Task<LookupSchedulingDecision> ScheduleAsync(
        CatalogSearchAttempt request,
        CancellationToken cancellationToken = default)
    {
        var matches = await musicCatalogCandidateSearch.SearchAsync(request.Query, cancellationToken);
        var localTrackForResolution = await TryLoadResolutionTrackAsync(request.Criteria, cancellationToken);
        var resolution = musicCatalogMatchResolver.Resolve(
            matches,
            new MusicCatalogResolutionContext(request.Query.Value, localTrackForResolution?.ReleaseDate));
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

            return new LookupSchedulingDecision(
                null,
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
            return new LookupSchedulingDecision(
                null,
                deferred.EstimatedRetryAfterSeconds,
                deferred.EarliestExpectedCompletionAt,
                deferred.Reason);
        }

        var budgetReservation = await reserveSourceApiBudgetPort.TryReserveAsync(
            new SourceApiBudgetReservationRequest(plannedLookup.Source, request.OccurredAt),
            cancellationToken);
        if (!budgetReservation.Accepted)
        {
            return new LookupSchedulingDecision(
                null,
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
            return new LookupSchedulingDecision(
                plannedLookup.Command,
                plan.EstimatedRetryAfterSeconds,
                plan.EarliestExpectedCompletionAt,
                plan.Reason);
        }

        var deferredByReservation = PriorityPlan.Defer(request.OccurredAt);
        return new LookupSchedulingDecision(
            null,
            deferredByReservation.EstimatedRetryAfterSeconds,
            deferredByReservation.EarliestExpectedCompletionAt,
            deferredByReservation.Reason);
    }

    private async Task<LocalMusicTrackSearchResult?> TryLoadResolutionTrackAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        const string trackPrefix = "track:";
        if (!criteria.Value.StartsWith(trackPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var trackId = criteria.Value[trackPrefix.Length..];
        return await localMusicTrackSearch.GetByMusicCatalogIdAsync(MusicCatalogId.From(trackId), cancellationToken);
    }

    private async Task PersistLifecycleAsync(
        CatalogSearchAttempt request,
        LookupSchedulingDecision result,
        CancellationToken cancellationToken)
    {
        await PersistDiscoveryAsync(
            request.Criteria,
            discovery =>
            {
                if (result.ShouldSchedule)
                {
                    var planned = discovery.Plan(
                        result.Command?.Priority ?? throw new InvalidOperationException("Scheduled discovery must include a priority."),
                        result.EstimatedRetryAfterSeconds,
                        result.EarliestExpectedCompletionAt,
                        result.Reason,
                        request.OccurredAt);

                    var started = discovery.Start(
                        result.Command?.Priority ?? throw new InvalidOperationException("Scheduled discovery must include a priority."),
                        "Lookup started",
                        request.OccurredAt);

                    return planned || started;
                }

                return discovery.Defer(
                    result.EstimatedRetryAfterSeconds,
                    result.EarliestExpectedCompletionAt,
                    result.Reason,
                    request.OccurredAt);
            },
            cancellationToken);
    }

    private Task PersistRejectedAsync(
        CatalogSearchAttempt request,
        MusicCatalogResolutionOutcome outcome,
        CancellationToken cancellationToken) =>
        PersistDiscoveryAsync(
            request.Criteria,
            discovery => discovery.Reject(ToRejectedReason(outcome), request.OccurredAt),
            cancellationToken);

    private async Task PersistDiscoveryAsync(
        CatalogSearchCriteria criteria,
        Func<CatalogSearchDiscovery, bool> apply,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, criteria, cancellationToken);
            if (!apply(discovery))
            {
                return;
            }

            if (await discovery.SaveAsync(discoveryRepository, cancellationToken))
            {
                return;
            }
        }

        throw new InvalidOperationException($"Unable to persist discovery lifecycle for {criteria.Value} after retry.");
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

    private static string ToRejectedReason(MusicCatalogResolutionOutcome outcome) =>
        outcome switch
        {
            MusicCatalogResolutionOutcome.NotFound => "Planner rejected lookup",
            MusicCatalogResolutionOutcome.Ambiguous => "Planner rejected lookup",
            _ => "Planner rejected lookup"
        };
}
