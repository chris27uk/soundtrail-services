using System.Collections.Concurrent;
using Soundtrail.Services.Features.Search.Contracts;

namespace Soundtrail.Services.Features.Search.Queueing;

public sealed class InMemoryEnqueueMusicRequest : IEnqueueMusicRequest
{
    private readonly ConcurrentQueue<LookupMusicRequest> requests = new();

    public Task EnqueueAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken)
    {
        requests.Enqueue(request);
        return Task.CompletedTask;
    }

    public ValueTask<LookupMusicRequest?> DequeueAsync(
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(
            requests.TryDequeue(out var request)
                ? request
                : null);
}
