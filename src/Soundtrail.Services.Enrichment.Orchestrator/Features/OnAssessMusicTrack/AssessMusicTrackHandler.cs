using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack;

public sealed class AssessMusicTrackHandler(
    IPotentialCatalogLookupWorkStore potentialCatalogLookupWorkStore,
    IPersistCatalogSearchDiscoveryPort persistCatalogSearchDiscoveryPort,
    DiscoveryPriorityPolicy discoveryPriorityPolicy,
    ILocalMusicTrackSearch localMusicTrackSearch)
{
    public async Task Handle(
        AssessMusicTrackCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Criteria is not null && command.TrustLevel is not null && command.RiskScore is not null)
        {
            var criteria = command.Criteria;
            await persistCatalogSearchDiscoveryPort.RequestAsync(
                criteria,
                command.TrustLevel.Value,
                command.RiskScore.Value,
                command.CreatedAt,
                command.CorrelationId,
                cancellationToken);
        }

        var candidate = await potentialCatalogLookupWorkStore.FindByMusicCatalogIdAsync(command.MusicCatalogId, cancellationToken);
        if (candidate is null)
        {
            return;
        }

        var assessment = discoveryPriorityPolicy.Assess(ToSummary(candidate), command.CreatedAt);
        if (assessment.Action != CatalogDiscoveryWorkAction.Schedule || assessment.Priority is null)
        {
            await persistCatalogSearchDiscoveryPort.ApplyToTrackingsAsync(
                command.MusicCatalogId,
                discovery => discovery.Defer(
                    assessment.EstimatedRetryAfterSeconds,
                    assessment.EarliestExpectedCompletionAt,
                    assessment.Reason,
                    command.CreatedAt),
                cancellationToken);
            return;
        }

        var localTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(command.MusicCatalogId, cancellationToken);
        if (localTrack?.IsPlayable == true)
        {
            await persistCatalogSearchDiscoveryPort.ApplyToTrackingsAsync(
                command.MusicCatalogId,
                discovery => discovery.Defer(
                    60,
                    command.CreatedAt.AddSeconds(60),
                    "Planner deferred lookup",
                    command.CreatedAt),
                cancellationToken);
            return;
        }

        await persistCatalogSearchDiscoveryPort.ApplyToTrackingsAsync(
            command.MusicCatalogId,
            discovery => discovery.Plan(
                assessment.Priority.Value,
                assessment.EstimatedRetryAfterSeconds,
                assessment.EarliestExpectedCompletionAt,
                assessment.Reason,
                command.CreatedAt),
            cancellationToken);
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
