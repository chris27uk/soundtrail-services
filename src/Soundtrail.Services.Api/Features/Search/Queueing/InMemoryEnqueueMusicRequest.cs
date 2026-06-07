using Soundtrail.Contracts;
using System.Collections.Concurrent;

namespace Soundtrail.Services.Api.Features.Search.Queueing;

public sealed class InMemoryEnqueueMusicRequest : IEnqueueMusicRequest
{
    private readonly ConcurrentQueue<LookupMusicRequest> requests = new();

    public Task EnqueueAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken)
    {
        this.requests.Enqueue(request);
        return Task.CompletedTask;
    }

    public ValueTask<LookupMusicRequest?> DequeueAsync(
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(
            this.requests.TryDequeue(out var request)
                ? request
                : null);
}
