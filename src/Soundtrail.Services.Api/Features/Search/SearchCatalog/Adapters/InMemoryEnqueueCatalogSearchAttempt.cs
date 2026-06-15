using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;
using Soundtrail.Services.Api.Features.Search.SearchCatalog.Ports;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using System.Collections.Concurrent;

namespace Soundtrail.Services.Api.Features.Search.SearchCatalog.Adapters;

public sealed class InMemoryEnqueueCatalogSearchAttempt : IEnqueueCatalogSearchAttempt
{
    private readonly ConcurrentQueue<CatalogSearchAttemptDto> requests = new();

    public List<CatalogSearchAttemptDto> Requests => this.requests.ToList();

    public Task EnqueueAsync(CatalogSearchAttempt request, CancellationToken cancellationToken)
    {
        this.requests.Enqueue(CatalogSearchAttemptMapper.ToDto(request));
        return Task.CompletedTask;
    }

    public ValueTask<CatalogSearchAttempt?> DequeueAsync(CancellationToken cancellationToken)
    {
        if (this.requests.TryDequeue(out var request))
        {
            return ValueTask.FromResult(CatalogSearchAttemptMapper.FromDto(request));
        }

        return ValueTask.FromResult<CatalogSearchAttempt?>(null);
    }
}
