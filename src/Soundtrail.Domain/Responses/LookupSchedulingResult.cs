using Soundtrail.Domain;
using Soundtrail.Domain.Commands;

namespace Soundtrail.Domain.Responses;

public sealed record LookupSchedulingResult(
    IReadOnlyList<ICommand> Commands,
    int? EstimatedRetryAfterSeconds,
    DateTimeOffset? EarliestExpectedCompletionAt,
    string Reason)
{
    public bool ShouldSchedule => Commands.Count > 0;

    public LookupPhaseCommand? Command => Commands.OfType<LookupPhaseCommand>().SingleOrDefault();

    public static LookupSchedulingResult DoNotSchedule(
        int? estimatedRetryAfterSeconds,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason) =>
        new([], estimatedRetryAfterSeconds, earliestExpectedCompletionAt, reason);

    public static LookupSchedulingResult Schedule(
        int? estimatedRetryAfterSeconds,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        params ICommand[] commands) =>
        new(commands, estimatedRetryAfterSeconds, earliestExpectedCompletionAt, reason);
}
