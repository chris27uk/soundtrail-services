using Soundtrail.Services.Enrichment.Jobs;

namespace Soundtrail.Services.Enrichment.Ports;

public interface IEnrichmentQueuePort
{
    Task EnqueueAsync(
        EnrichmentJob job,
        CancellationToken cancellationToken);
}
