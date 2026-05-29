using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;
using Soundtrail.Services.Enrichment.Infrastructure.Scheduling;
using System.Collections.Concurrent;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Orchestration;

public sealed class AzureServiceBusEnrichmentQueue : IEnrichmentQueuePort
{
    private readonly ConcurrentQueue<EnrichmentJob> jobs = new();

    public Task EnqueueAsync(
        EnrichmentJob job,
        CancellationToken cancellationToken)
    {
        jobs.Enqueue(job);
        return Task.CompletedTask;
    }
}
