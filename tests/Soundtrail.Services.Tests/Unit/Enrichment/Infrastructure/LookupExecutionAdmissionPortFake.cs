using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class LookupExecutionAdmissionPortFake : ILookupExecutionAdmissionPort
{
    private readonly Dictionary<string, Queue<LookupExecutionAdmissionResult>> resultsByProvider = [];
    private readonly HashSet<string> activeCommands = [];
    private readonly HashSet<string> committedCommands = [];

    public List<LookupExecutionAdmissionRequest> Requests { get; } = [];

    public Task<LookupExecutionAdmissionResult> TryAcquireAsync(
        LookupExecutionAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (committedCommands.Contains(request.CommandId.Value) || activeCommands.Contains(request.CommandId.Value))
        {
            return Task.FromResult(LookupExecutionAdmissionResult.Duplicate());
        }

        if (resultsByProvider.TryGetValue(request.Provider.Value, out var configured) && configured.Count > 0)
        {
            var result = configured.Dequeue();
            if (result.Status == LookupExecutionAdmissionStatus.Acquired)
            {
                activeCommands.Add(request.CommandId.Value);
            }

            return Task.FromResult(result);
        }

        activeCommands.Add(request.CommandId.Value);
        return Task.FromResult(LookupExecutionAdmissionResult.Acquired());
    }

    public Task CommitAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        activeCommands.Remove(commandId.Value);
        committedCommands.Add(commandId.Value);
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        activeCommands.Remove(commandId.Value);
        return Task.CompletedTask;
    }

    public void Reject(
        LookupSource provider,
        DateTimeOffset retryAt,
        string reason)
    {
        if (!resultsByProvider.TryGetValue(provider.Value, out var results))
        {
            results = [];
            resultsByProvider[provider.Value] = results;
        }

        results.Enqueue(LookupExecutionAdmissionResult.Deferred(retryAt, reason));
    }

    public void RejectAfterSuccesses(
        LookupSource provider,
        int successfulAcquisitions)
    {
        if (!resultsByProvider.TryGetValue(provider.Value, out var results))
        {
            results = [];
            resultsByProvider[provider.Value] = results;
        }

        for (var i = 0; i < successfulAcquisitions; i++)
        {
            results.Enqueue(LookupExecutionAdmissionResult.Acquired());
        }

        results.Enqueue(LookupExecutionAdmissionResult.Deferred(
            DateTimeOffset.UtcNow.AddMinutes(1),
            $"{provider.Value} budget temporarily unavailable"));
    }
}
