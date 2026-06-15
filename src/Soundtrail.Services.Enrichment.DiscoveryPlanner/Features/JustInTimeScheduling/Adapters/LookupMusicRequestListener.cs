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

public sealed class LookupMusicRequestListener(
    LookupMusicRequestHandler handler,
    IUpsertDiscoveryStatusPort upsertDiscoveryStatusPort)
{
    private const int PlannedRetryAfterSeconds = 30;
    private const int DeferredRetryAfterSeconds = 60;

    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        LookupMusicRequestDto requestDto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var request = new LookupMusicRequest(
            DiscoveryQueryKey.From(requestDto.QueryKey),
            NormalizedSearchQuery.FromText(requestDto.Query),
            requestDto.TrustLevel,
            requestDto.RiskScore,
            requestDto.OccurredAt,
            CorrelationId.From(requestDto.CorrelationId));
        var queryKey = request.QueryKey;

        try
        {
            var result = await handler.ScheduleAsync(request, cancellationToken);
            await upsertDiscoveryStatusPort.UpsertAsync(ToStatusUpdate(queryKey, result, request), cancellationToken);
            return result.Commands.Select(command => command.ToMessage()).ToArray();
        }
        catch (ResolutionFailedException ex)
        {
            await upsertDiscoveryStatusPort.UpsertAsync(
                new DiscoveryStatusUpdate(
                    queryKey,
                    DiscoveryLifecycleStatus.Rejected,
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

    private static DiscoveryStatusUpdate ToStatusUpdate(
        DiscoveryQueryKey queryKey,
        Soundtrail.Domain.Responses.LookupSchedulingResult result,
        LookupMusicRequest request)
    {
        if (result.ShouldSchedule)
        {
            return new DiscoveryStatusUpdate(
                queryKey,
                DiscoveryLifecycleStatus.Planned,
                result.Command?.Priority,
                WillBeLookedUp: true,
                EstimatedRetryAfterSeconds: PlannedRetryAfterSeconds,
                EarliestExpectedCompletionAt: null,
                Reason: "Planner queued lookup",
                UpdatedAt: request.OccurredAt);
        }

        return new DiscoveryStatusUpdate(
            queryKey,
            DiscoveryLifecycleStatus.Deferred,
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
