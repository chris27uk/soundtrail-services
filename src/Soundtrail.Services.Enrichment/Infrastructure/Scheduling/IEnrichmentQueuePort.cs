using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Infrastructure.Scheduling;

public interface IEnrichmentQueuePort
{
    Task EnqueueAsync(
        EnrichmentJob job,
        CancellationToken cancellationToken);
}
