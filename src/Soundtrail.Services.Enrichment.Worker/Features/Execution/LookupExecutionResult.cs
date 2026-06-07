using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.Execution;

public sealed record LookupExecutionResult(LookupExecutionOutcome Outcome, EnrichmentResponse? Response)
{
    public static LookupExecutionResult Completed(EnrichmentResponse response) => new(LookupExecutionOutcome.Completed, response);

    public static LookupExecutionResult Duplicate() => new(LookupExecutionOutcome.Duplicate, null);
}
