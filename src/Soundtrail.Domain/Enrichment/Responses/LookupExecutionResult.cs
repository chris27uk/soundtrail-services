using Soundtrail.Domain.Responses;

namespace Soundtrail.Domain.Responses;

public sealed record LookupExecutionResult(
    LookupExecutionOutcome Outcome,
    EnrichmentResponse? Response,
    string? Reason,
    DateTimeOffset? RetryAt,
    int? RetryAfterSeconds)
{
    public static LookupExecutionResult Completed(EnrichmentResponse response) =>
        new(LookupExecutionOutcome.Completed, response, null, null, null);

    public static LookupExecutionResult Deferred(
        string reason,
        DateTimeOffset? retryAt,
        int? retryAfterSeconds) =>
        new(LookupExecutionOutcome.Deferred, null, reason, retryAt, retryAfterSeconds);

    public static LookupExecutionResult Duplicate() =>
        new(LookupExecutionOutcome.Duplicate, null, null, null, null);

    public static LookupExecutionResult Failed(string reason) =>
        new(LookupExecutionOutcome.Failed, null, reason, null, null);
}
