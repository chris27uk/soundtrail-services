using System.Collections.Concurrent;
using Soundtrail.Services.EnrichmentWorker.Jobs;
using Soundtrail.Services.EnrichmentWorker.Ports;

namespace Soundtrail.Services.EnrichmentWorker.Infrastructure.ServiceBus;

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
