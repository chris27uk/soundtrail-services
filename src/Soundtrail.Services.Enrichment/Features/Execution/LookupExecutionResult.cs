using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Features.Execution;

public sealed record LookupExecutionResult(LookupExecutionOutcome Outcome)
{
    public static LookupExecutionResult Completed() => new(LookupExecutionOutcome.Completed);

    public static LookupExecutionResult Duplicate() => new(LookupExecutionOutcome.Duplicate);
}
