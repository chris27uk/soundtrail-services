using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Discovery.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Collaborators;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Collaborators;

/// <summary>
/// Runs lookup attempts one-at-a-time so Completions do not race on the same discovery stream.
/// </summary>
internal static class LookupPlanContinuation
{
    public static IMessage? PrepareNext(
        DiscoveryHistory aggregate,
        LookupResult result)
    {
        if (result is LookupResult.Succeeded)
        {
            return null;
        }

        var originalCommandId = GetOriginalCommandId(result);
        var scheduled = aggregate.Events
            .OfType<WorkScheduled>()
            .LastOrDefault(work => MatchesDispatch(work, originalCommandId));
        if (scheduled is null)
        {
            return null;
        }

        if (aggregate.IsWorkCompleted(scheduled.Target))
        {
            return null;
        }

        var dispatch = CreateDispatch(scheduled);
        var plan = LookupPlanningPolicy.Build(dispatch);
        if (plan.Attempts.Count == 0)
        {
            return null;
        }

        var attemptIndex = IndexOfAttempt(plan, dispatch, originalCommandId);
        if (attemptIndex < 0)
        {
            return null;
        }

        if (result is LookupResult.Deferred)
        {
            return WorkerCommandFactory.Create(dispatch, plan.Attempts[attemptIndex]);
        }

        if (result is LookupResult.Duplicate)
        {
            return null;
        }

        if (attemptIndex < plan.Attempts.Count - 1)
        {
            return WorkerCommandFactory.Create(dispatch, plan.Attempts[attemptIndex + 1]);
        }

        // Exhaustion must be in the same append as the final FailAttempt — a second Save with the
        // same Completions OperationId would be dropped as DuplicateOperation.
        aggregate.ApplyWorkedCompleted(
            scheduled.Target,
            scheduled.Priority,
            "All lookup attempts exhausted.",
            GetCompletedAt(result));
        return null;
    }

    private static bool MatchesDispatch(WorkScheduled work, MessageId originalCommandId)
    {
        var dispatchCommandId = MessageId.Deterministic(
            "DispatchLookupWork",
            work.Target.NormalisedIdentifier,
            work.ScheduledAt.ToString("O"));
        return originalCommandId.Value.StartsWith($"{dispatchCommandId.Value}:", StringComparison.Ordinal);
    }

    private static DispatchLookupWork CreateDispatch(WorkScheduled scheduled) =>
        new(
            scheduled.Target,
            scheduled.Priority,
            MessageId.Deterministic(
                "DispatchLookupWork",
                scheduled.Target.NormalisedIdentifier,
                scheduled.ScheduledAt.ToString("O")),
            CorrelationId.From($"work-scheduled:{scheduled.Target.NormalisedIdentifier}:{scheduled.ScheduledAt:O}"),
            scheduled.ScheduledAt);

    private static int IndexOfAttempt(
        LookupPlan plan,
        DispatchLookupWork dispatch,
        MessageId originalCommandId)
    {
        for (var index = 0; index < plan.Attempts.Count; index++)
        {
            var command = WorkerCommandFactory.Create(dispatch, plan.Attempts[index]);
            if (command.Id == originalCommandId)
            {
                return index;
            }
        }

        return -1;
    }

    private static MessageId GetOriginalCommandId(LookupResult result) =>
        result.Match(
            succeeded => succeeded.Context.OriginalCommandId,
            duplicate => duplicate.Context.OriginalCommandId,
            notFound => notFound.Context.OriginalCommandId,
            deferred => deferred.Context.OriginalCommandId,
            failed => failed.Context.OriginalCommandId);

    private static DateTimeOffset GetCompletedAt(LookupResult result) =>
        result.Match(
            succeeded => succeeded.CompletedAt,
            duplicate => duplicate.CompletedAt,
            notFound => notFound.CompletedAt,
            deferred => deferred.CompletedAt,
            failed => failed.CompletedAt);
}
