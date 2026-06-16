using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;

public sealed class CatalogSearchAttemptListener(
    CatalogSearchAttemptHandler handler,
    ICatalogSearchDiscoveryRepository discoveryRepository)
{
    private const int PlannedRetryAfterSeconds = 30;
    private const int DeferredRetryAfterSeconds = 60;

    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        CatalogSearchAttemptDto requestDto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var request = new CatalogSearchAttempt(
            CatalogSearchCriteria.From(requestDto.Criteria),
            NormalizedSearchQuery.FromText(requestDto.Query),
            requestDto.TrustLevel,
            requestDto.RiskScore,
            requestDto.OccurredAt,
            CorrelationId.From(requestDto.CorrelationId));
        var criteria = request.Criteria;

        try
        {
            var result = await handler.ScheduleAsync(request, cancellationToken);
            await PersistLifecycleAsync(criteria, discovery =>
            {
                if (result.ShouldSchedule)
                {
                    return discovery.Plan(
                        result.Command?.Priority ?? throw new InvalidOperationException("Scheduled discovery must include a priority."),
                        PlannedRetryAfterSeconds,
                        earliestExpectedCompletionAt: null,
                        reason: "Planner queued lookup",
                        plannedAt: request.OccurredAt);
                }

                return discovery.Defer(
                    DeferredRetryAfterSeconds,
                    request.OccurredAt.AddSeconds(DeferredRetryAfterSeconds),
                    "Planner deferred lookup",
                    request.OccurredAt);
            }, cancellationToken);

            return result.Commands.Select(command => command.ToMessage()).ToArray();
        }
        catch (ResolutionFailedException ex)
        {
            await PersistLifecycleAsync(
                criteria,
                discovery => discovery.Reject(ToRejectedReason(ex.Outcome), request.OccurredAt),
                cancellationToken);

            return [];
        }
    }

    private async Task PersistLifecycleAsync(
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

    private static string ToRejectedReason(MusicCatalogResolutionOutcome outcome) =>
        outcome switch
        {
            MusicCatalogResolutionOutcome.NotFound => "Planner rejected lookup",
            MusicCatalogResolutionOutcome.Ambiguous => "Planner rejected lookup",
            _ => "Planner rejected lookup"
        };
}
