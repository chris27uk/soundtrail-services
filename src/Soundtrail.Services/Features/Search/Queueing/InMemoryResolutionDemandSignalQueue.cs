using System.Collections.Concurrent;
using Soundtrail.Services.Features.Search.Contracts;

namespace Soundtrail.Services.Features.Search.Queueing;

public sealed class InMemoryResolutionDemandSignalQueue : IResolutionDemandSignalPort
{
    private readonly ConcurrentQueue<ResolutionDemandSignal> signals = new();

    public Task EnqueueAsync(
        ResolutionDemandSignal signal,
        CancellationToken cancellationToken)
    {
        signals.Enqueue(signal);
        return Task.CompletedTask;
    }

    public ValueTask<ResolutionDemandSignal?> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(
            signals.TryDequeue(out var signal)
                ? signal
                : null);
    }
}
