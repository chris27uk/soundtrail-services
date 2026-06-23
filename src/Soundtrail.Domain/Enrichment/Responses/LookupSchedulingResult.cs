using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Responses;

public sealed record LookupSchedulingResult(
    bool ShouldSchedule,
    LookupPriorityBand? Priority,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason)
{
    public static LookupSchedulingResult DoNotSchedule(
        int? estimatedRetryAfterSeconds,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason) =>
        new(false, null, estimatedRetryAfterSeconds, earliestExpectedCompletionAt, reason);

    public static LookupSchedulingResult Schedule(
        LookupPriorityBand priority,
        int? estimatedRetryAfterSeconds,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason) =>
        new(true, priority, estimatedRetryAfterSeconds, earliestExpectedCompletionAt, reason);
}
