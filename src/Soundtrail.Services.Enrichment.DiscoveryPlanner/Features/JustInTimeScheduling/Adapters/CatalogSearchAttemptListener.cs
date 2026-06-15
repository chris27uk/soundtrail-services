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
    IUpsertCatalogSearchStatusPort upsertDiscoveryStatusPort)
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
            await upsertDiscoveryStatusPort.UpsertAsync(ToStatusUpdate(criteria, result, request), cancellationToken);
            return result.Commands.Select(command => command.ToMessage()).ToArray();
        }
        catch (ResolutionFailedException ex)
        {
            await upsertDiscoveryStatusPort.UpsertAsync(
                new CatalogSearchStatusUpdate(
                    criteria,
                    CatalogSearchLifecycleStatus.Rejected,
                    Priority: null,
                    WillBeLookedUp: false,
                    EstimatedRetryAfterSeconds: null,
                    EarliestExpectedCompletionAt: null,
                    Reason: ToRejectedReason(ex.Outcome),
                    UpdatedAt: request.OccurredAt),
                cancellationToken);

            return [];
        }
    }

    private static CatalogSearchStatusUpdate ToStatusUpdate(
        CatalogSearchCriteria criteria,
        Soundtrail.Domain.Responses.LookupSchedulingResult result,
        CatalogSearchAttempt request)
    {
        if (result.ShouldSchedule)
        {
            return new CatalogSearchStatusUpdate(
                criteria,
                CatalogSearchLifecycleStatus.Planned,
                result.Command?.Priority,
                WillBeLookedUp: true,
                EstimatedRetryAfterSeconds: PlannedRetryAfterSeconds,
                EarliestExpectedCompletionAt: null,
                Reason: "Planner queued lookup",
                UpdatedAt: request.OccurredAt);
        }

        return new CatalogSearchStatusUpdate(
            criteria,
            CatalogSearchLifecycleStatus.Deferred,
            Priority: null,
            WillBeLookedUp: true,
            EstimatedRetryAfterSeconds: DeferredRetryAfterSeconds,
            EarliestExpectedCompletionAt: request.OccurredAt.AddSeconds(DeferredRetryAfterSeconds),
            Reason: "Planner deferred lookup",
            UpdatedAt: request.OccurredAt);
    }

    private static string ToRejectedReason(MusicCatalogResolutionOutcome outcome) =>
        outcome switch
        {
            MusicCatalogResolutionOutcome.NotFound => "Planner rejected lookup",
            MusicCatalogResolutionOutcome.Ambiguous => "Planner rejected lookup",
            _ => "Planner rejected lookup"
        };
}
