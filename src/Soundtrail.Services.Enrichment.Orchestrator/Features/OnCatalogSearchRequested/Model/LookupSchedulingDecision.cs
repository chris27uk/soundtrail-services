using Soundtrail.Domain.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Model;

internal sealed record LookupSchedulingDecision(
    IMusicCatalogLookupCommand? Command,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason)
{
    public bool ShouldSchedule => Command is not null;
}
