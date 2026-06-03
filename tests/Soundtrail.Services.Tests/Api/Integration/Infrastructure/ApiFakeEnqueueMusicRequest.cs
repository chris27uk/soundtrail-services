using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Queueing;

namespace Soundtrail.Services.Tests.Api.Integration.Infrastructure;

public sealed class ApiFakeEnqueueMusicRequest : IEnqueueMusicRequest
{
    private readonly Queue<LookupMusicRequest> requests = new();

    public IReadOnlyList<LookupMusicRequest> EnqueuedRequests => this.requests.ToArray();

    public Task EnqueueAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken)
    {
        this.requests.Enqueue(request);
        return Task.CompletedTask;
    }
}
