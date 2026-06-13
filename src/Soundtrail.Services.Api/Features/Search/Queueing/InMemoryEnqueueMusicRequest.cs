using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using System.Collections.Concurrent;

namespace Soundtrail.Services.Api.Features.Search.Queueing;

public sealed class InMemoryEnqueueMusicRequest : IEnqueueMusicRequest
{
    private readonly ConcurrentQueue<LookupMusicRequestDto> requests = new();
    
    public List<LookupMusicRequestDto> Requests => this.requests.ToList();

    public Task EnqueueAsync(LookupMusicRequest request, CancellationToken cancellationToken)
    {
        this.requests.Enqueue(LookupMusicRequestMapper.ToDto(request));
        return Task.CompletedTask;
    }

    public ValueTask<LookupMusicRequest?> DequeueAsync(CancellationToken cancellationToken)
    {
        if (this.requests.TryDequeue(out var request))
        {
            return ValueTask.FromResult(LookupMusicRequestMapper.FromDto(request));
        }
        
        return ValueTask.FromResult<LookupMusicRequest?>(null);
    }
}
