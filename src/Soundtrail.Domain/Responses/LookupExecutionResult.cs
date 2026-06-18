using Soundtrail.Domain.Responses;

namespace Soundtrail.Domain.Responses;

public sealed record LookupExecutionResult(LookupExecutionOutcome Outcome, EnrichmentResponse? Response)
{
    public static LookupExecutionResult Completed(EnrichmentResponse response) => new(LookupExecutionOutcome.Completed, response);

    public static LookupExecutionResult Deferred() => new(LookupExecutionOutcome.Deferred, (EnrichmentResponse?)null);

    public static LookupExecutionResult Duplicate() => new(LookupExecutionOutcome.Duplicate, (EnrichmentResponse?)null);
}
