using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Queueing;

namespace Soundtrail.Services.Tests.Integration.Features.Search;

public sealed class ApiFakeLookupMusicRequestQueue : ILookupMusicRequestQueue
{
    private readonly Queue<LookupMusicRequest> requests = new();

    public IReadOnlyList<LookupMusicRequest> EnqueuedRequests => requests.ToArray();

    public Task EnqueueAsync(
        LookupMusicRequest request,
        CancellationToken cancellationToken)
    {
        requests.Enqueue(request);
        return Task.CompletedTask;
    }
}
