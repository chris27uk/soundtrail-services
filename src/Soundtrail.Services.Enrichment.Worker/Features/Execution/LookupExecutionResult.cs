using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

using Soundtrail.Contracts.Worker.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.Execution;

public sealed record LookupExecutionResult(LookupExecutionOutcome Outcome, EnrichmentResponseDto? Response)
{
    public static LookupExecutionResult Completed(EnrichmentResponseDto response) => new(LookupExecutionOutcome.Completed, response);

    public static LookupExecutionResult Duplicate() => new(LookupExecutionOutcome.Duplicate, null);
}
