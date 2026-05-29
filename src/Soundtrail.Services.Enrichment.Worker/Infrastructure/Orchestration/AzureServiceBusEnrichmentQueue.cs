using System.Collections.Concurrent;
using Soundtrail.Services.Enrichment.Jobs;
using Soundtrail.Services.Enrichment.Ports;

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
