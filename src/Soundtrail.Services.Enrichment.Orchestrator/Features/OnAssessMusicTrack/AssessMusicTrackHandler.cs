using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack;

public sealed class AssessMusicTrackHandler(
    IPotentialCatalogLookupWorkStore potentialCatalogLookupWorkStore,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    ILocalMusicTrackSearch localMusicTrackSearch) : IHandler<AssessMusicTrackCommand>
{
    public async Task Handle(AssessMusicTrackCommand command, CancellationToken cancellationToken = default)
    {
        if (command.SearchTerm is not null && command.TrustLevel is not null && command.RiskScore is not null)
        {
            var loaded = await SearchOrSeekHistory.LoadAsync(
                discoveryRepository,
                command.SearchTerm,
                cancellationToken);

            if (loaded.Aggregate.Request(
                    command.SearchTerm,
                    null,
                    command.TrustLevel.Value,
                    command.RiskScore.Value,
                    command.CreatedAt,
                    command.CorrelationId))
            {
                await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
            }
        }

        var candidate = await potentialCatalogLookupWorkStore.FindByMusicCatalogIdAsync(command.MusicCatalogId, cancellationToken);
        if (candidate is null)
        {
            return;
        }

        var assessment = discoveryPriorityPolicy.Assess(ToSummary(candidate), command.CreatedAt);
        if (assessment.Action != CatalogDiscoveryWorkAction.Schedule || assessment.Priority is null)
        {
            var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(command.MusicCatalogId, cancellationToken);
            foreach (var tracking in trackings)
            {
                var loaded = await SearchOrSeekHistory.LoadAsync(discoveryRepository, tracking.SearchCriteria, cancellationToken);
                if (!loaded.Aggregate.Defer(
                        assessment.EstimatedRetryAfterSeconds,
                        assessment.EarliestExpectedCompletionAt,
                        assessment.Reason,
                        command.CreatedAt))
                {
                    continue;
                }

                await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
            }
            return;
        }

        var localTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(command.MusicCatalogId, cancellationToken);
        if (localTrack?.IsPlayable == true)
        {
            var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(command.MusicCatalogId, cancellationToken);
            foreach (var tracking in trackings)
            {
                var loaded = await SearchOrSeekHistory.LoadAsync(discoveryRepository, tracking.SearchCriteria, cancellationToken);
                if (!loaded.Aggregate.Defer(
                        60,
                        command.CreatedAt.AddSeconds(60),
                        "Planner deferred lookup",
                        command.CreatedAt))
                {
                    continue;
                }

                await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
            }
            return;
        }

        var finalTrackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(command.MusicCatalogId, cancellationToken);
        foreach (var tracking in finalTrackings)
        {
            var loaded = await SearchOrSeekHistory.LoadAsync(discoveryRepository, tracking.SearchCriteria, cancellationToken);
            if (!loaded.Aggregate.Plan(
                    assessment.Priority.Value,
                    assessment.EstimatedRetryAfterSeconds,
                    assessment.EarliestExpectedCompletionAt,
                    assessment.Reason,
                    command.CreatedAt))
            {
                continue;
            }

            await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
        }
    }

    private static CatalogDiscoveryWorkSummary ToSummary(PotentialCatalogLookupWork candidate) =>
        new(
            candidate.MusicCatalogId,
            candidate.RequestCount,
            candidate.HighestTrustLevelSeen,
            candidate.RiskScore,
            candidate.Status switch
            {
                PotentialCatalogLookupWorkStatus.Pending => CatalogDiscoveryWorkStatus.Pending,
                PotentialCatalogLookupWorkStatus.Ignored => CatalogDiscoveryWorkStatus.Ignored,
                PotentialCatalogLookupWorkStatus.Resolved => CatalogDiscoveryWorkStatus.Resolved,
                _ => throw new ArgumentOutOfRangeException(nameof(candidate.Status), candidate.Status, null)
            },
            candidate.NextEligibleAt,
            Priority: null,
            Reason: null);
}
