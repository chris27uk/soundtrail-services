using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogDiscoveryAssessmentResult(
    CatalogDiscoveryWorkAction? Action,
    LookupPriorityBand? Priority,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string? Reason)
{
    public static CatalogDiscoveryAssessmentResult Noop() =>
        new(
            Action: null,
            Priority: null,
            EstimatedRetryAfterSeconds: null,
            EarliestExpectedCompletionAt: null,
            Reason: null);

    public void ApplyTo(
        SearchDiscoveryHistory discovery,
        MusicSearchCriteria? searchCriteria,
        int? trustLevel,
        int? riskScore,
        DateTimeOffset occurredAt,
        CorrelationId correlationId)
    {
        if (searchCriteria is not null && trustLevel is not null && riskScore is not null)
        {
            discovery.Request(
                searchCriteria,
                playback: null,
                trustLevel.Value,
                riskScore.Value,
                occurredAt,
                correlationId);
        }

        switch (Action)
        {
            case CatalogDiscoveryWorkAction.Schedule when Priority is not null:
                discovery.Plan(
                    Priority.Value,
                    EstimatedRetryAfterSeconds,
                    EarliestExpectedCompletionAt,
                    Reason ?? "Planner queued lookup",
                    occurredAt);
                break;
            case CatalogDiscoveryWorkAction.Defer:
            case CatalogDiscoveryWorkAction.Ignore:
                discovery.Defer(
                    EstimatedRetryAfterSeconds,
                    EarliestExpectedCompletionAt,
                    Reason ?? "Planner deferred lookup",
                    occurredAt);
                break;
        }
    }
}
