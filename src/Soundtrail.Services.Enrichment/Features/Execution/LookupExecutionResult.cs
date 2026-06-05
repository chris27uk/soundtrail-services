using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Features.Execution;

public sealed record LookupExecutionResult(
    LookupExecutionOutcome Outcome,
    EnrichmentResponse? Response)
{
    public static LookupExecutionResult Completed(EnrichmentResponse response) => new(LookupExecutionOutcome.Completed, response);

    public static LookupExecutionResult Duplicate() => new(LookupExecutionOutcome.Duplicate, null);
}
