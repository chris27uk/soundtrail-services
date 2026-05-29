using Soundtrail.Services.EnrichmentWorker.Jobs;

namespace Soundtrail.Services.EnrichmentWorker.Ports;

public interface IEnrichmentQueuePort
{
    Task EnqueueAsync(
        EnrichmentJob job,
        CancellationToken cancellationToken);
}
